using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinControl.Infrastructure.Persistence.DesignTime;

public sealed class FinControlDbContextFactory : IDesignTimeDbContextFactory<FinControlDbContext>
{
    public FinControlDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<FinControlDbContext>();
        var databasePath = ObterCaminhoBancoDesignTime();

        optionsBuilder.UseSqlite($"Data Source={databasePath}");

        return new FinControlDbContext(optionsBuilder.Options);
    }

    private static string ObterCaminhoBancoDesignTime()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FinControl");

        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "fincontrol-design.db");
    }
}
