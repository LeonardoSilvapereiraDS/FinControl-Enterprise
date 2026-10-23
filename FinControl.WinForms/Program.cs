using System.Globalization;
using FinControl.WinForms.Desktop;

namespace FinControl.WinForms;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("pt-BR");
        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (_, e) => DesktopUi.Error(null, e.Exception);
        var smoke = args.Contains("--smoke-test");
        if (smoke) Environment.SetEnvironmentVariable("FINCONTROL_DATA_DIR", Path.Combine(Path.GetTempPath(), "FinControl-smoke-" + Guid.NewGuid().ToString("N")));
        using var instance = new Mutex(true, smoke ? "Local\\FinControlSmoke-" + Guid.NewGuid().ToString("N") : "Local\\FinControlEnterpriseDesktop", out var first);
        if (!first) { MessageBox.Show("O FinControl já está aberto. Confira a barra de tarefas.", "FinControl Enterprise"); return; }
        try
        {
            DesktopTheme.Load();
            using var window = new DesktopForm(smoke);
            System.Windows.Forms.Application.Run(window);
        }
        catch (Exception ex) { DesktopUi.Error(null, ex); }
        finally { instance.ReleaseMutex(); }
    }
}
