using System.Globalization;
using FinControl.Application.Categorias;
using FinControl.Application.Contas;
using FinControl.Application.Dashboard;
using FinControl.Application.Transacoes;
using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;
using FinControl.Infrastructure.Persistence;
using FinControl.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace FinControl.Web.Services;

public static class FinanceEndpoints
{
    public static void MapFinance(this WebApplication app)
    {
        var api = app.MapGroup("/api/w/{workspaceId:int}").RequireAuthorization().AddEndpointFilter<WorkspaceFilter>();
        api.MapGet("/state", async (string? month, HttpContext h, FinControlDbContext db, ICategoriaService cats, IContaService accts, ITransacaoService txs, IDashboardService dashboards) =>
        {
            if (!DateTime.TryParseExact(month ?? DateTime.Today.ToString("yyyy-MM"), "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date.Year < 1901 || date.Year > 9997)
                throw new RegraDeNegocioException("Mês inválido.");
            var owner = h.OwnerId();
            var transactions = await txs.ListarAsync(owner); var categories = await cats.ListarAsync(owner,true); var accounts = await accts.ListarAsync(owner,true);
            var dashboard = await dashboards.ObterDashboardAsync(owner,date);
            var goals = await db.MetasFinanceiras.Where(x=>x.UsuarioId==owner).ToListAsync();
            var budgets = await db.Orcamentos.Where(x=>x.UsuarioId==owner).ToListAsync();
            var recurring = await db.Recorrencias.Where(x=>x.UsuarioId==owner).ToListAsync();
            return Results.Ok(new { workspace=h.Items["workspace"], role=h.Role(), transactions, categories,
                accounts=accounts.Select(a=> new { a.Id,a.Nome,a.TipoConta,a.SaldoInicial,a.Ativa, saldo=a.SaldoInicial+transactions.Where(t=>t.ContaId==a.Id&&t.Pago).Sum(t=>t.Tipo==TipoTransacao.Receita?t.Valor:-t.Valor) }),
                dashboard, goals, budgets, recurring });
        });
        api.MapPost("/transactions", async (SalvarTransacaoRequest r,HttpContext h,ITransacaoService s) => Results.Ok(await s.CriarAsync(h.OwnerId(),r)));
        api.MapPut("/transactions/{id:int}", async (int id,SalvarTransacaoRequest r,HttpContext h,ITransacaoService s) => Results.Ok(await s.AtualizarAsync(h.OwnerId(),id,r)));
        api.MapDelete("/transactions/{id:int}", async (int id,HttpContext h,ITransacaoService s) => { await s.RemoverAsync(h.OwnerId(),id); return Results.NoContent(); });
        api.MapPatch("/transactions/{id:int}/paid", async (int id,PagoRequest r,HttpContext h,FinControlDbContext db) =>
        {
            var t=await db.Transacoes.SingleOrDefaultAsync(x=>x.Id==id&&x.UsuarioId==h.OwnerId()) ?? throw new ApiException(404,"Lançamento não encontrado.");
            if(r.Pago)t.MarcarComoPago();else t.MarcarComoPendente(); await db.SaveChangesAsync();return Results.NoContent();
        });
        api.MapPost("/categories", async (SalvarCategoriaRequest r,HttpContext h,ICategoriaService s) => Results.Ok(await s.CriarAsync(h.OwnerId(),r)));
        api.MapPut("/categories/{id:int}", async (int id,SalvarCategoriaRequest r,HttpContext h,ICategoriaService s,FinControlDbContext db) =>
        {
            var c=await db.Categorias.SingleOrDefaultAsync(x=>x.Id==id&&x.UsuarioId==h.OwnerId());
            if(c is not null&&c.Tipo!=r.Tipo&&await db.Recorrencias.AnyAsync(x=>x.CategoriaId==id))throw new RegraDeNegocioException("Categoria utilizada em recorrência. Crie uma nova categoria para outro tipo.");
            return Results.Ok(await s.AtualizarAsync(h.OwnerId(),id,r));
        });
        api.MapPatch("/categories/{id:int}", async (int id,SituacaoRequest r,HttpContext h,ICategoriaService s) => { if(r.Ativa)await s.ReativarAsync(h.OwnerId(),id);else await s.DesativarAsync(h.OwnerId(),id);return Results.NoContent(); });
        api.MapPost("/accounts", async (SalvarContaRequest r,HttpContext h,IContaService s) => Results.Ok(await s.CriarAsync(h.OwnerId(),r)));
        api.MapPut("/accounts/{id:int}", async (int id,SalvarContaRequest r,HttpContext h,IContaService s) => Results.Ok(await s.AtualizarAsync(h.OwnerId(),id,r)));
        api.MapPatch("/accounts/{id:int}", async (int id,SituacaoRequest r,HttpContext h,IContaService s) => { if(r.Ativa)await s.ReativarAsync(h.OwnerId(),id);else await s.DesativarAsync(h.OwnerId(),id);return Results.NoContent(); });
        api.MapPost("/goals", async (MetaRequest r,HttpContext h,FinControlDbContext db) =>
        {
            var goal=new MetaFinanceira(r.Nome,r.Descricao,r.ValorObjetivo,r.ValorAtual,r.DataInicio,r.DataLimite,h.OwnerId());db.MetasFinanceiras.Add(goal);await db.SaveChangesAsync();return Results.Ok(goal);
        });
        api.MapPut("/goals/{id:int}", async (int id,MetaRequest r,HttpContext h,FinControlDbContext db) =>
        {
            _=new MetaFinanceira(r.Nome,r.Descricao,r.ValorObjetivo,r.ValorAtual,r.DataInicio,r.DataLimite,h.OwnerId());
            var g=await db.MetasFinanceiras.SingleOrDefaultAsync(x=>x.Id==id&&x.UsuarioId==h.OwnerId())??throw new ApiException(404,"Meta não encontrada.");
            g.AtualizarDados(r.Nome,r.Descricao,r.ValorObjetivo,r.DataInicio,r.DataLimite);g.AtualizarValorAtual(r.ValorAtual);await db.SaveChangesAsync();return Results.Ok(g);
        });
        api.MapDelete("/goals/{id:int}",async(int id,HttpContext h,FinControlDbContext db)=>{var x=await db.MetasFinanceiras.SingleOrDefaultAsync(x=>x.Id==id&&x.UsuarioId==h.OwnerId())??throw new ApiException(404,"Meta não encontrada.");db.Remove(x);await db.SaveChangesAsync();return Results.NoContent();});
        api.MapPost("/budgets", async(OrcamentoRequest r,HttpContext h,FinControlDbContext db)=>
        {
            var c=await db.Categorias.SingleOrDefaultAsync(x=>x.Id==r.CategoriaId&&x.UsuarioId==h.OwnerId()&&x.Ativa)??throw new RegraDeNegocioException("Selecione uma categoria ativa.");
            if(await db.Orcamentos.AnyAsync(x=>x.UsuarioId==h.OwnerId()&&x.CategoriaId==r.CategoriaId&&x.Mes==r.Mes&&x.Ano==r.Ano))throw new ApiException(409,"Esta categoria já tem orçamento neste mês. Edite o orçamento existente.");
            var b=new Orcamento(c.Id,c.Tipo,h.OwnerId(),r.ValorLimite,r.Mes,r.Ano);db.Orcamentos.Add(b);await db.SaveChangesAsync();return Results.Ok(b);
        });
        api.MapPut("/budgets/{id:int}",async(int id,OrcamentoRequest r,HttpContext h,FinControlDbContext db)=>
        {
            var b=await db.Orcamentos.SingleOrDefaultAsync(x=>x.Id==id&&x.UsuarioId==h.OwnerId())??throw new ApiException(404,"Orçamento não encontrado.");
            if(b.CategoriaId!=r.CategoriaId)throw new RegraDeNegocioException("A categoria não pode ser alterada. Crie outro orçamento.");
            if(await db.Orcamentos.AnyAsync(x=>x.Id!=id&&x.UsuarioId==h.OwnerId()&&x.CategoriaId==r.CategoriaId&&x.Mes==r.Mes&&x.Ano==r.Ano))throw new ApiException(409,"Já existe um orçamento para a categoria neste mês.");
            b.Atualizar(r.ValorLimite,r.Mes,r.Ano);await db.SaveChangesAsync();return Results.Ok(b);
        });
        api.MapDelete("/budgets/{id:int}",async(int id,HttpContext h,FinControlDbContext db)=>{var x=await db.Orcamentos.SingleOrDefaultAsync(x=>x.Id==id&&x.UsuarioId==h.OwnerId())??throw new ApiException(404,"Orçamento não encontrado.");db.Remove(x);await db.SaveChangesAsync();return Results.NoContent();});
        api.MapPost("/recurring",async(RecorrenciaRequest r,HttpContext h,RecurringService s)=>{var rule=await s.Create(h.OwnerId(),r);await s.Generate(Today(),h.OwnerId());return Results.Ok(rule);});
        api.MapPatch("/recurring/{id:int}",async(int id,SituacaoRequest r,HttpContext h,FinControlDbContext db)=>
        {
            var rule=await db.Recorrencias.SingleOrDefaultAsync(x=>x.Id==id&&x.UsuarioId==h.OwnerId())??throw new ApiException(404,"Recorrência não encontrada.");
            if(r.Ativa&&!rule.Ativa) { while(rule.ProximaData<Today())rule.ProximaData=RecurringService.Next(rule);if(rule.Fim is not null&&rule.ProximaData>rule.Fim)throw new RegraDeNegocioException("Esta recorrência já terminou. Cadastre outra."); }
            rule.Ativa=r.Ativa;await db.SaveChangesAsync();return Results.NoContent();
        });
        api.MapPost("/recurring/generate",async(HttpContext h,RecurringService s)=>Results.Ok(new {gerados=await s.Generate(Today(),h.OwnerId())}));
        api.MapPost("/import",async(ImportarRequest r,HttpContext h,ImportService s)=>Results.Ok(await s.Import(h.OwnerId(),r)));
        api.MapGet("/backup",async(HttpContext h,BackupService s)=>{h.Admin();return Results.Ok(await s.Export((EspacoFinanceiro)h.Items["workspace"]!));});
        app.MapPost("/api/restore",async(BackupDocument r,HttpContext h,BackupService s)=>Results.Ok(new {id=await s.Restore(h.AccountId(),r)})).RequireAuthorization();
        api.MapPut("/settings",async(EspacoRequest r,HttpContext h,FinControlDbContext db)=>
        {
            h.Admin();var name=WebSupport.Text(r.Nome,"Nome do espaço");var profile=WebSupport.Profile(r.Perfil);var s=(EspacoFinanceiro)h.Items["workspace"]!;s.Nome=name;s.Perfil=profile;await db.SaveChangesAsync();return Results.Ok(s);
        });
        api.MapGet("/members",async(HttpContext h,FinControlDbContext db)=>
        {
            h.Admin();var sid=((EspacoFinanceiro)h.Items["workspace"]!).Id;
            return Results.Ok(await(from m in db.Membros join a in db.AcessosWeb on m.AcessoId equals a.Id where m.EspacoId==sid select new {m.Id,m.AcessoId,a.Nome,a.Email,m.Papel}).ToListAsync());
        });
        api.MapPost("/members",async(MembroRequest r,HttpContext h,FinControlDbContext db)=>
        {
            h.Admin();var email=WebSupport.Email(r.Email);var role=WebSupport.MemberRole(r.Papel);var sid=((EspacoFinanceiro)h.Items["workspace"]!).Id;
            var a=await db.AcessosWeb.SingleOrDefaultAsync(x=>x.Email==email)??throw new RegraDeNegocioException("Essa pessoa precisa criar uma conta neste FinControl antes de ser adicionada.");
            var m=await db.Membros.SingleOrDefaultAsync(x=>x.EspacoId==sid&&x.AcessoId==a.Id);
            if(m is null){m=new MembroEspaco {EspacoId=sid,AcessoId=a.Id,Papel=role};db.Membros.Add(m);}
            else {if(m.Papel=="Administrador"&&role!="Administrador"&&await db.Membros.CountAsync(x=>x.EspacoId==sid&&x.Papel=="Administrador")==1)throw new RegraDeNegocioException("O espaço precisa de pelo menos um administrador.");m.Papel=role;}
            await db.SaveChangesAsync();return Results.Ok(new {m.Id});
        });
        api.MapDelete("/members/{id:int}",async(int id,HttpContext h,FinControlDbContext db)=>
        {
            h.Admin();var sid=((EspacoFinanceiro)h.Items["workspace"]!).Id;var m=await db.Membros.SingleOrDefaultAsync(x=>x.Id==id&&x.EspacoId==sid)??throw new ApiException(404,"Membro não encontrado.");
            if(m.Papel=="Administrador"&&await db.Membros.CountAsync(x=>x.EspacoId==sid&&x.Papel=="Administrador")==1)throw new RegraDeNegocioException("Não é possível remover o último administrador.");
            db.Membros.Remove(m);await db.SaveChangesAsync();return Results.NoContent();
        });
    }
    public static DateTime Today()=>TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo")).Date;
}
