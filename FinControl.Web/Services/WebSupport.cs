using System.Net.Mail;
using System.Security.Claims;
using FinControl.Domain.Entities;
using FinControl.Domain.Exceptions;
using FinControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace FinControl.Web.Services;

public sealed class ApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
public sealed class WriteGate { public SemaphoreSlim Semaphore { get; } = new(1, 1); }
public sealed class InitialSetup { public string Code { get; } = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(12)); }

public static class WebSupport
{
    public static int AccountId(this HttpContext h) => int.Parse(h.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public static int OwnerId(this HttpContext h) => ((EspacoFinanceiro)h.Items["workspace"]!).UsuarioId;
    public static string Role(this HttpContext h) => (string)h.Items["role"]!;
    public static void Admin(this HttpContext h)
    {
        if (h.Role() != "Administrador") throw new ApiException(403, "Apenas administradores podem realizar esta ação.");
    }
    public static string Text(string? value, string label, int max = 120)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0 || text.Length > max) throw new RegraDeNegocioException($"{label}: informe entre 1 e {max} caracteres.");
        return text;
    }
    public static string Email(string? value)
    {
        var email = Text(value, "E-mail", 180).ToLowerInvariant();
        if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email || !email.Contains('.'))
            throw new RegraDeNegocioException("Informe um e-mail válido.");
        return email;
    }
    public static void Password(string? value)
    {
        if (value is null || value.Length < 10 || value.Length > 128 || string.IsNullOrWhiteSpace(value))
            throw new RegraDeNegocioException("A senha deve ter entre 10 e 128 caracteres. Prefira uma frase longa.");
    }
    public static string Profile(string? profile) => profile is "Pessoal" or "MEI" or "Empresa" ? profile : throw new RegraDeNegocioException("Perfil inválido.");
    public static string MemberRole(string? role) => role is "Administrador" or "Editor" or "Leitor" ? role : throw new RegraDeNegocioException("Permissão inválida.");
}
public sealed class WorkspaceFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext c, EndpointFilterDelegate next)
    {
        var h = c.HttpContext;
        if (!int.TryParse(h.Request.RouteValues["workspaceId"]?.ToString(), out var id)) throw new ApiException(404, "Espaço não encontrado.");
        var db = h.RequestServices.GetRequiredService<FinControlDbContext>();
        var member = await db.Membros.SingleOrDefaultAsync(x => x.EspacoId == id && x.AcessoId == h.AccountId());
        if (member is null) throw new ApiException(404, "Espaço não encontrado ou sem acesso.");
        if (!HttpMethods.IsGet(h.Request.Method) && member.Papel == "Leitor") throw new ApiException(403, "Seu acesso é somente de leitura.");
        h.Items["workspace"] = (await db.Espacos.FindAsync(id))!;
        h.Items["role"] = member.Papel;
        return await next(c);
    }
}
