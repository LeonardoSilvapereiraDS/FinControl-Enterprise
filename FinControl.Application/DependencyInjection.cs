using FinControl.Application.Categorias;
using FinControl.Application.Contas;
using FinControl.Application.Dashboard;
using FinControl.Application.Transacoes;
using Microsoft.Extensions.DependencyInjection;

namespace FinControl.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFinControlApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<IContaService, ContaService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITransacaoService, TransacaoService>();

        return services;
    }
}
