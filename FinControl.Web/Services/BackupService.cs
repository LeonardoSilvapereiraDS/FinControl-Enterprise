using FinControl.Application.Categorias;
using FinControl.Application.Contas;
using FinControl.Application.Transacoes;
using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;
using FinControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace FinControl.Web.Services;

public sealed record BackupMeta(string Nome, string? Descricao, decimal ValorObjetivo, decimal ValorAtual, DateTime DataInicio, DateTime DataLimite);
public sealed record BackupOrcamento(int CategoriaId, decimal ValorLimite, int Mes, int Ano);
public sealed record BackupRecorrencia(int Id, string Descricao, decimal Valor, int Tipo, int CategoriaId, int ContaId, string Frequencia, DateTime Inicio, DateTime? Fim, DateTime ProximaData, bool Ativa);
public sealed record BackupOcorrencia(int RecorrenciaId, DateTime Data, int TransacaoId);
public sealed record BackupImportacao(string Chave, int TransacaoId);
public sealed record BackupDocument(int Versao, DateTime ExportadoEm, string Nome, string Perfil, List<CategoriaDto> Categorias, List<ContaDto> Contas, List<TransacaoDto> Transacoes,
    List<BackupMeta> Metas, List<BackupOrcamento> Orcamentos, List<BackupRecorrencia> Recorrencias, List<BackupOcorrencia> Ocorrencias, List<BackupImportacao> Importacoes);

public sealed class BackupService(FinControlDbContext db, WorkspaceService spaces, ICategoriaService categories, IContaService accounts, ITransacaoService transactions)
{
    public async Task<BackupDocument> Export(EspacoFinanceiro space)
    {
        var owner = space.UsuarioId;
        var cats = (await categories.ListarAsync(owner, true)).ToList();
        var accts = (await accounts.ListarAsync(owner, true)).ToList();
        var ledger = (await transactions.ListarAsync(owner)).ToList();
        var goals = await db.MetasFinanceiras.Where(x => x.UsuarioId == owner).Select(x => new BackupMeta(x.Nome,x.Descricao,x.ValorObjetivo,x.ValorAtual,x.DataInicio,x.DataLimite)).ToListAsync();
        var budgets = await db.Orcamentos.Where(x => x.UsuarioId == owner).Select(x => new BackupOrcamento(x.CategoriaId,x.ValorLimite,x.Mes,x.Ano)).ToListAsync();
        var rules = await db.Recorrencias.Where(x => x.UsuarioId == owner).Select(x => new BackupRecorrencia(x.Id,x.Descricao,x.Valor,x.Tipo,x.CategoriaId,x.ContaId,x.Frequencia,x.Inicio,x.Fim,x.ProximaData,x.Ativa)).ToListAsync();
        var ids = rules.Select(x => x.Id).ToArray();
        var occurrences = await db.Ocorrencias.Where(x => ids.Contains(x.RecorrenciaId)).Select(x => new BackupOcorrencia(x.RecorrenciaId,x.Data,x.TransacaoId)).ToListAsync();
        var imports = await db.Importacoes.Where(x => x.UsuarioId == owner).Select(x => new BackupImportacao(x.Chave,x.TransacaoId)).ToListAsync();
        return new(1,DateTime.UtcNow,space.Nome,space.Perfil,cats,accts,ledger,goals,budgets,rules,occurrences,imports);
    }
    public async Task<int> Restore(int actor, BackupDocument b)
    {
        if (b.Versao != 1 || b.Categorias is null || b.Contas is null || b.Transacoes is null || b.Metas is null || b.Orcamentos is null || b.Recorrencias is null || b.Ocorrencias is null || b.Importacoes is null)
            throw new RegraDeNegocioException("Backup inválido ou versão não suportada.");
        if (b.Categorias.Count + b.Contas.Count + b.Transacoes.Count + b.Metas.Count + b.Orcamentos.Count + b.Recorrencias.Count + b.Ocorrencias.Count + b.Importacoes.Count > 30000)
            throw new RegraDeNegocioException("O backup excede 30.000 registros. Restaure-o com suporte técnico.");
        static void ValidIds(IEnumerable<int> ids) { var l = ids.ToList(); if (l.Any(x => x <= 0) || l.Distinct().Count() != l.Count) throw new RegraDeNegocioException("O backup contém identificadores inválidos ou repetidos."); }
        ValidIds(b.Categorias.Select(x=>x.Id)); ValidIds(b.Contas.Select(x=>x.Id)); ValidIds(b.Transacoes.Select(x=>x.Id)); ValidIds(b.Recorrencias.Select(x=>x.Id));
        await using var tx = await db.Database.BeginTransactionAsync();
        var name = WebSupport.Text(b.Nome,"Nome do backup");
        var space = await spaces.Create(actor, name[..Math.Min(name.Length,100)] + " (restaurado)", b.Perfil, seed: false);
        var owner = space.UsuarioId;
        var cats = new Dictionary<int,Categoria>(); var accts = new Dictionary<int,Conta>(); var ledger = new Dictionary<int,int>(); var rules = new Dictionary<int,Recorrencia>();
        foreach(var c in b.Categorias) { var x = new Categoria(c.Nome,c.Tipo,owner); if(!c.Ativa) x.Desativar(); db.Categorias.Add(x); cats.Add(c.Id,x); }
        foreach(var c in b.Contas) { var x = new Conta(c.Nome,c.TipoConta,c.SaldoInicial,owner); if(!c.Ativa) x.Desativar(); db.Contas.Add(x); accts.Add(c.Id,x); }
        await db.SaveChangesAsync();
        foreach(var t in b.Transacoes)
        {
            if(!cats.TryGetValue(t.CategoriaId,out var c) || !accts.TryGetValue(t.ContaId,out var a)) throw new RegraDeNegocioException("O backup contém lançamentos sem categoria ou conta.");
            var x = new Transacao(t.Descricao,t.Valor,t.Data,t.Tipo,c.Id,c.Tipo,a.Id,owner,t.Observacao,t.Pago,t.Recorrente);
            db.Transacoes.Add(x); await db.SaveChangesAsync(); ledger.Add(t.Id,x.Id);
        }
        foreach(var g in b.Metas) db.MetasFinanceiras.Add(new MetaFinanceira(g.Nome,g.Descricao,g.ValorObjetivo,g.ValorAtual,g.DataInicio,g.DataLimite,owner));
        foreach(var o in b.Orcamentos)
        {
            if(!cats.TryGetValue(o.CategoriaId,out var c)) throw new RegraDeNegocioException("Orçamento sem categoria no backup.");
            db.Orcamentos.Add(new Orcamento(c.Id,c.Tipo,owner,o.ValorLimite,o.Mes,o.Ano));
        }
        foreach(var r in b.Recorrencias)
        {
            if(!cats.TryGetValue(r.CategoriaId,out var c) || !accts.TryGetValue(r.ContaId,out var a)) throw new RegraDeNegocioException("Recorrência sem conta ou categoria no backup.");
            _ = new Transacao(r.Descricao,r.Valor,r.Inicio,(TipoTransacao)r.Tipo,c.Id,c.Tipo,a.Id,owner);
            if(r.Frequencia is not ("Mensal" or "Semanal" or "Anual") || r.Inicio.Year < 2000 || r.Inicio.Year > 2100 || r.ProximaData < r.Inicio || r.ProximaData.Year > 2101 || (r.Fim is not null && (r.Fim < r.Inicio || r.Fim.Value.Year > 2100))) throw new RegraDeNegocioException("Recorrência inválida no backup.");
            var x = new Recorrencia { UsuarioId=owner, Descricao=r.Descricao, Valor=r.Valor, Tipo=r.Tipo, CategoriaId=c.Id, ContaId=a.Id, Frequencia=r.Frequencia, Inicio=r.Inicio.Date, Fim=r.Fim?.Date, ProximaData=r.ProximaData.Date, Ativa=r.Ativa };
            db.Recorrencias.Add(x); rules.Add(r.Id,x);
        }
        await db.SaveChangesAsync();
        foreach(var o in b.Ocorrencias)
        {
            if(!rules.TryGetValue(o.RecorrenciaId,out var r) || o.Data < r.Inicio || o.Data >= r.ProximaData) throw new RegraDeNegocioException("Histórico de recorrência inválido no backup.");
            db.Ocorrencias.Add(new OcorrenciaRecorrente { RecorrenciaId=r.Id, Data=o.Data.Date, TransacaoId=ledger.GetValueOrDefault(o.TransacaoId) });
        }
        foreach(var imported in b.Importacoes)
        {
            var parts = (imported.Chave ?? "").Split(':');
            if(parts.Length != 2 || !int.TryParse(parts[0], out var oldAccount) || !accts.TryGetValue(oldAccount, out var account) || parts[1].Length != 64 || !parts[1].All(Uri.IsHexDigit))
                throw new RegraDeNegocioException("Histórico de importação inválido no backup.");
            db.Importacoes.Add(new RegistroImportado { UsuarioId=owner, Chave=account.Id+":"+parts[1], TransacaoId=ledger.GetValueOrDefault(imported.TransacaoId) });
        }
        await db.SaveChangesAsync(); await tx.CommitAsync(); return space.Id;
    }
}
