using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FinControl.Domain.Entities;
using FinControl.Infrastructure.Persistence;
using FinControl.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace FinControl.Web.Services;

public static class AuthEndpoints
{
    public static void MapAuth(this WebApplication app)
    {
        app.MapGet("/api/session", async (HttpContext h, FinControlDbContext db, WorkspaceService spaces) =>
        {
            h.Response.Headers.CacheControl = "no-store";
            if (h.User.Identity?.IsAuthenticated != true)
                return Results.Ok(new { user = (object?)null, workspaces = Array.Empty<object>(), requiresSetup = !await db.AcessosWeb.AnyAsync() && await db.Contas.AnyAsync() });
            var user = await db.AcessosWeb.FindAsync(h.AccountId());
            return Results.Ok(new { user = (object)new { user!.Id, user.Nome, user.Email }, workspaces = await spaces.List(user.Id), requiresSetup = false });
        });
        app.MapPost("/api/auth/register", async (CadastroRequest r, HttpContext h, FinControlDbContext db, WorkspaceService spaces, InitialSetup setup, IPasswordHasher<AcessoWeb> hasher) =>
        {
            var name = WebSupport.Text(r.Nome, "Nome"); var email = WebSupport.Email(r.Email); WebSupport.Password(r.Senha);
            WebSupport.Profile(r.Perfil); WebSupport.Text(r.Espaco, "Nome do espaço");
            var first = !await db.AcessosWeb.AnyAsync();
            var legacy = first && await db.Contas.AnyAsync();
            if (legacy && !CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(r.CodigoInicial ?? "")), SHA256.HashData(Encoding.UTF8.GetBytes(setup.Code))))
                throw new ApiException(400, "Informe o código inicial exibido no computador que iniciou o FinControl para proteger os dados existentes.");
            if (await db.AcessosWeb.AnyAsync(x => x.Email == email)) throw new ApiException(409, "Este e-mail já está cadastrado. Entre na sua conta.");
            await using var tx = await db.Database.BeginTransactionAsync();
            var user = new AcessoWeb { Nome = name, Email = email };
            user.SenhaHash = hasher.HashPassword(user, r.Senha); db.AcessosWeb.Add(user); await db.SaveChangesAsync();
            await spaces.Create(user.Id, r.Espaco, r.Perfil);
            if (legacy)
            {
                var oldUsers = await db.Usuarios.Where(x => !db.Espacos.Any(s => s.UsuarioId == x.Id)).ToListAsync();
                foreach (var old in oldUsers) await spaces.Create(user.Id, "Dados anteriores — " + old.Nome, "Pessoal", old.Id);
            }
            await tx.CommitAsync(); await SignIn(h, user); return Results.Ok(new { message = "Conta criada." });
        }).RequireRateLimiting("auth");
        app.MapPost("/api/auth/login", async (LoginRequest r, HttpContext h, FinControlDbContext db, IPasswordHasher<AcessoWeb> hasher) =>
        {
            var email = (r.Email ?? "").Trim().ToLowerInvariant();
            if (email.Length > 180 || r.Senha is null || r.Senha.Length > 128) throw new ApiException(401, "E-mail ou senha incorretos.");
            var user = await db.AcessosWeb.SingleOrDefaultAsync(x => x.Email == email);
            if (user?.BloqueadoAte > DateTime.UtcNow) throw new ApiException(429, "Muitas tentativas. Aguarde 15 minutos e tente novamente.");
            // O hash fictício evita diferenças grandes no tempo de resposta para e-mails não cadastrados.
            var hashUser = user ?? new AcessoWeb();
            var hash = user?.SenhaHash ?? hasher.HashPassword(hashUser, "Credencial fictícia para comparação");
            var result = hasher.VerifyHashedPassword(hashUser, hash, r.Senha);
            if (user is null || result == PasswordVerificationResult.Failed)
            {
                if (user is not null) { user.Tentativas++; if (user.Tentativas >= 5) { user.BloqueadoAte = DateTime.UtcNow.AddMinutes(15); user.Tentativas = 0; } await db.SaveChangesAsync(); }
                throw new ApiException(401, "E-mail ou senha incorretos.");
            }
            user.Tentativas = 0; user.BloqueadoAte = null;
            if (result == PasswordVerificationResult.SuccessRehashNeeded) user.SenhaHash = hasher.HashPassword(user, r.Senha);
            await db.SaveChangesAsync(); await SignIn(h, user); return Results.Ok(new { message = "Bem-vindo." });
        }).RequireRateLimiting("auth");
        app.MapPost("/api/auth/logout", async (HttpContext h) => { await h.SignOutAsync(); return Results.NoContent(); });
        app.MapPut("/api/auth/password", async (SenhaRequest r, HttpContext h, FinControlDbContext db, IPasswordHasher<AcessoWeb> hasher) =>
        {
            WebSupport.Password(r.NovaSenha);
            var user = (await db.AcessosWeb.FindAsync(h.AccountId()))!;
            if (r.SenhaAtual is null || r.SenhaAtual.Length > 128 || hasher.VerifyHashedPassword(user, user.SenhaHash, r.SenhaAtual) == PasswordVerificationResult.Failed)
                throw new ApiException(400, "A senha atual está incorreta.");
            user.SenhaHash = hasher.HashPassword(user, r.NovaSenha); user.SeloSeguranca = Guid.NewGuid().ToString("N");
            await db.SaveChangesAsync(); await SignIn(h, user); return Results.Ok(new { message = "Senha alterada. As outras sessões foram encerradas." });
        }).RequireAuthorization().RequireRateLimiting("auth");
        app.MapPost("/api/workspaces", async (EspacoRequest r, HttpContext h, FinControlDbContext db, WorkspaceService spaces) =>
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var space = await spaces.Create(h.AccountId(), r.Nome, r.Perfil); await tx.CommitAsync();
            return Results.Ok(new { space.Id });
        }).RequireAuthorization();
    }
    static Task SignIn(HttpContext h, AcessoWeb user) => h.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim("stamp", user.SeloSeguranca) }, CookieAuthenticationDefaults.AuthenticationScheme)),
        new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(12) });
}
