using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;
using FinControl.Infrastructure.Persistence;
using FinControl.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace FinControl.Web.Services;

public sealed class RecurringService(FinControlDbContext db)
{
    public async Task<Recorrencia> Create(int owner, RecorrenciaRequest r)
    {
        var category = await db.Categorias.SingleOrDefaultAsync(x => x.Id == r.CategoriaId && x.UsuarioId == owner && x.Ativa);
        var account = await db.Contas.SingleOrDefaultAsync(x => x.Id == r.ContaId && x.UsuarioId == owner && x.Ativa);
        if (category is null || account is null) throw new RegraDeNegocioException("Escolha uma conta e uma categoria ativas deste espaço.");
        _ = new Transacao(r.Descricao, r.Valor, r.Inicio, r.Tipo, r.CategoriaId, category.Tipo, r.ContaId, owner);
        if (r.Frequencia is not ("Mensal" or "Semanal" or "Anual")) throw new RegraDeNegocioException("Frequência inválida.");
        if (r.Inicio.Year < 2000 || r.Inicio.Year > 2100 || (r.Fim is not null && (r.Fim < r.Inicio || r.Fim.Value.Year > 2100)))
            throw new RegraDeNegocioException("A recorrência deve ter datas entre 2000 e 2100 e término posterior ao início.");
        var rule = new Recorrencia { UsuarioId = owner, Descricao = r.Descricao.Trim(), Valor = r.Valor, Tipo = (int)r.Tipo, CategoriaId = r.CategoriaId, ContaId = r.ContaId,
            Frequencia = r.Frequencia, Inicio = r.Inicio.Date, Fim = r.Fim?.Date, ProximaData = r.Inicio.Date };
        db.Recorrencias.Add(rule); await db.SaveChangesAsync(); return rule;
    }
    public static DateTime Next(Recorrencia r)
    {
        if (r.Frequencia == "Semanal") return r.ProximaData.AddDays(7);
        var next = r.Frequencia == "Anual" ? r.ProximaData.AddYears(1) : r.ProximaData.AddMonths(1);
        var month = r.Frequencia == "Anual" ? r.Inicio.Month : next.Month;
        return new DateTime(next.Year, month, Math.Min(r.Inicio.Day, DateTime.DaysInMonth(next.Year, month)));
    }
    public async Task<int> Generate(DateTime today, int? owner = null)
    {
        var rules = await db.Recorrencias.Where(x => x.Ativa && (owner == null || x.UsuarioId == owner)).ToListAsync();
        int generated = 0;
        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var rule in rules)
        {
            var category = await db.Categorias.FindAsync(rule.CategoriaId);
            var account = await db.Contas.FindAsync(rule.ContaId);
            if (category?.Ativa != true || account?.Ativa != true) continue;
            int limit = 0;
            while (rule.ProximaData <= today.Date && (rule.Fim == null || rule.ProximaData <= rule.Fim) && limit++ < 6000)
            {
                if (!await db.Ocorrencias.AnyAsync(x => x.RecorrenciaId == rule.Id && x.Data == rule.ProximaData))
                {
                    var transaction = new Transacao(rule.Descricao, rule.Valor, rule.ProximaData, (TipoTransacao)rule.Tipo, rule.CategoriaId, category.Tipo, rule.ContaId, rule.UsuarioId,
                        "Gerada automaticamente por uma recorrência.", pago: false, recorrente: true);
                    db.Transacoes.Add(transaction); await db.SaveChangesAsync();
                    db.Ocorrencias.Add(new OcorrenciaRecorrente { RecorrenciaId = rule.Id, Data = rule.ProximaData, TransacaoId = transaction.Id }); generated++;
                }
                rule.ProximaData = Next(rule);
            }
            if (rule.Fim is not null && rule.ProximaData > rule.Fim) rule.Ativa = false;
        }
        await db.SaveChangesAsync(); await tx.CommitAsync(); return generated;
    }
}
public sealed class RecurringWorker(IServiceScopeFactory scopes, WriteGate gate, ILogger<RecurringWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await gate.Semaphore.WaitAsync(stoppingToken);
                try
                {
                    using var scope = scopes.CreateScope();
                    var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
                    var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
                    await scope.ServiceProvider.GetRequiredService<RecurringService>().Generate(today);
                }
                finally { gate.Semaphore.Release(); }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { log.LogError(ex, "Falha ao gerar recorrências. Nova tentativa em um minuto."); }
            try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
