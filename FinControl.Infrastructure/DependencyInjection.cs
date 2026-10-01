using FinControl.Application.Abstractions.Persistence;
using FinControl.Infrastructure.Persistence;
using FinControl.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FinControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFinControlInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<FinControlDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
