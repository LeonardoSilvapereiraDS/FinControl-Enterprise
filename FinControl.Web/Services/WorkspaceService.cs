using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace FinControl.Web.Services;

public sealed class WorkspaceService(FinControlDbContext db)
{
    public async Task<EspacoFinanceiro> Create(int accessId, string name, string profile, int? legacyUser = null, bool seed = true)
    {
        name = WebSupport.Text(name, "Nome do espaço"); profile = WebSupport.Profile(profile);
        var user = legacyUser is null ? new Usuario(name, $"{Guid.NewGuid():N}@workspace.fincontrol.local", "sem-login") : await db.Usuarios.FindAsync(legacyUser.Value);
        if (legacyUser is null) { db.Usuarios.Add(user!); await db.SaveChangesAsync(); }
        var space = new EspacoFinanceiro { Nome = name, Perfil = profile, UsuarioId = user!.Id };
        db.Espacos.Add(space); await db.SaveChangesAsync();
        db.Membros.Add(new MembroEspaco { EspacoId = space.Id, AcessoId = accessId, Papel = "Administrador" });
        if (legacyUser is null && seed)
        {
            db.Contas.Add(new Conta(profile == "Pessoal" ? "Minha carteira" : "Caixa principal", TipoConta.Dinheiro, 0, user.Id));
            var receitas = profile == "Pessoal" ? new[]{"Salário", "Renda extra", "Outras receitas"} : new[]{"Vendas", "Serviços", "Outras receitas"};
            var despesas = profile == "Pessoal" ? new[]{"Alimentação", "Moradia", "Transporte", "Saúde", "Lazer", "Outras despesas"} : new[]{"Fornecedores", "Aluguel", "Pessoal", "Tributos", "Marketing", "Outras despesas"};
            db.Categorias.AddRange(receitas.Select(x => new Categoria(x, TipoCategoria.Receita, user.Id)));
            db.Categorias.AddRange(despesas.Select(x => new Categoria(x, TipoCategoria.Despesa, user.Id)));
        }
        await db.SaveChangesAsync(); return space;
    }
    public async Task<object[]> List(int accessId) => await (
        from m in db.Membros join s in db.Espacos on m.EspacoId equals s.Id
        where m.AcessoId == accessId orderby s.Nome
        select new { s.Id, s.Nome, s.Perfil, m.Papel }).Cast<object>().ToArrayAsync();
}
