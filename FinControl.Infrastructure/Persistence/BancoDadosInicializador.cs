using FinControl.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinControl.Infrastructure.Persistence;

public static class BancoDadosInicializador
{
    public const int UsuarioPadraoId = 1;

    public static void Inicializar(FinControlDbContext context)
    {
        context.Database.Migrate();

        if (context.Usuarios.Any())
        {
            return;
        }

        context.Usuarios.Add(new Usuario(
            "Usuario Local",
            "usuario@fincontrol.local",
            "local"));

        context.SaveChanges();
    }
}
