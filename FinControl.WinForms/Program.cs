using FinControl.Application;
using FinControl.Infrastructure;
using FinControl.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace FinControl.WinForms;

static class Program
{
    private static ServiceProvider? _serviceProvider;

    [STAThread]
    static void Main()
    {
        try
        {
            System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            var services = new ServiceCollection();
            ConfigurarServicos(services);

            _serviceProvider = services.BuildServiceProvider();
            InicializarBancoDados(_serviceProvider);

            System.Windows.Forms.Application.ApplicationExit += (_, _) => _serviceProvider.Dispose();
            System.Windows.Forms.Application.Run(_serviceProvider.GetRequiredService<MainForm>());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.ToString(),
                "Erro ao iniciar o FinControl",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private static void ConfigurarServicos(IServiceCollection services)
    {
        services
            .AddFinControlApplication()
            .AddFinControlInfrastructure(ObterConnectionString());

        services.AddTransient<DashboardControl>();
        services.AddTransient<TransacoesControl>();
        services.AddTransient<CategoriasControl>();
        services.AddTransient<ContasControl>();
        services.AddTransient<MainForm>();
    }

    private static string ObterConnectionString()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FinControl");

        Directory.CreateDirectory(directory);

        var databasePath = Path.Combine(directory, "fincontrol.db");

        return $"Data Source={databasePath}";
    }

    private static void InicializarBancoDados(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FinControlDbContext>();

        BancoDadosInicializador.Inicializar(context);
    }
}
