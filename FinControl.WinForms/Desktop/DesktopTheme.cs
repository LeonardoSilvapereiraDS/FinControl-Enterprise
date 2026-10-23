using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FinControl.WinForms.Desktop;

internal static class DesktopTheme
{
    public static string Mode { get; private set; } = "Sistema";
    public static bool Dark { get; private set; }
    public static Color Background => ColorTranslator.FromHtml(Dark ? "#101827" : "#F3F6FB");
    public static Color Surface => ColorTranslator.FromHtml(Dark ? "#1B2638" : "#FFFFFF");
    public static Color Text => ColorTranslator.FromHtml(Dark ? "#E8EEF8" : "#17263D");
    public static Color Muted => ColorTranslator.FromHtml(Dark ? "#AAB8CC" : "#5C6E85");
    public static Color Border => ColorTranslator.FromHtml(Dark ? "#34445A" : "#DEE5EF");
    public static Color Accent => ColorTranslator.FromHtml(Dark ? "#2DD4BF" : "#0F766E");
    public static Color Negative => ColorTranslator.FromHtml(Dark ? "#FDA4AF" : "#BE123C");
    public static Color Sidebar => Color.FromArgb(15, 27, 47);
    private static string Preference => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FinControl", "desktop-theme.txt");
    public static void Load()
    {
        try { if (File.Exists(Preference)) Mode = File.ReadAllText(Preference).Trim(); } catch (IOException) { }
        Resolve();
    }
    public static void Set(string mode)
    {
        Mode = mode; Resolve();
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Preference)!);
        File.WriteAllText(Preference, Mode);
    }
    private static void Resolve()
    {
        Dark = Mode == "Escuro";
        if (Mode == "Sistema")
            Dark = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int value && value == 0;
    }
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    public static void Apply(Control root)
    {
        root.BackColor = root is TextBoxBase or ComboBox or NumericUpDown or DataGridView ? Surface : Background;
        root.ForeColor = Text;
        if (root is Button button)
        {
            bool primary = Equals(button.Tag, "primary");
            button.BackColor = primary ? Accent : Surface; button.ForeColor = primary ? (Dark ? Sidebar : Color.White) : Text;
            button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderColor = Border;
        }
        if (root is Label label && Equals(label.Tag, "muted")) label.ForeColor = Muted;
        if (root is DataGridView grid)
        {
            grid.BackgroundColor = Surface; grid.GridColor = Border;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Background;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
            grid.DefaultCellStyle.BackColor = Surface; grid.DefaultCellStyle.ForeColor = Text;
            grid.DefaultCellStyle.SelectionBackColor = Dark ? Color.FromArgb(35, 72, 83) : Color.FromArgb(218, 242, 237);
            grid.DefaultCellStyle.SelectionForeColor = Text;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Dark ? Color.FromArgb(24, 36, 54) : Color.FromArgb(248, 250, 253);
        }
        foreach (Control child in root.Controls) Apply(child);
        if (root is Form form && form.IsHandleCreated)
        { int dark = Dark ? 1 : 0; DwmSetWindowAttribute(form.Handle, 20, ref dark, sizeof(int)); }
        root.Invalidate(true);
    }
}

internal static class DesktopUi
{
    public static Label Label(string text, float size = 10, bool bold = false) => new()
    { Text = text, AutoSize = true, Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(0, 6, 8, 6) };
    public static Button Button(string text, Func<Task> action, bool primary = false)
    {
        var button = new Button { Text = text, AutoSize = true, MinimumSize = new Size(100, 38), Padding = new Padding(10, 4, 10, 4), Margin = new Padding(0, 3, 8, 3), Cursor = Cursors.Hand, Tag = primary ? "primary" : null };
        button.Click += async (_, _) =>
        {
            button.Enabled = false;
            try { await action(); } catch (Exception ex) { Error(button.FindForm(), ex); }
            finally { if (!button.IsDisposed) button.Enabled = true; }
        };
        return button;
    }
    public static void Error(IWin32Window? owner, Exception exception) => MessageBox.Show(owner,
        exception is HttpRequestException ? "Não foi possível acessar o serviço local. Feche e abra o FinControl novamente." : exception.Message,
        "FinControl • Não foi possível concluir", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    public static bool Confirm(IWin32Window owner, string text) => MessageBox.Show(owner, text, "Confirmar ação", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    public static void Clear(Control control) { while (control.Controls.Count > 0) { var child = control.Controls[0]; control.Controls.Remove(child); child.Dispose(); } }
    public static DataGridView Grid() => new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false, AutoGenerateColumns = false, RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, BorderStyle = BorderStyle.None,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersHeight = 42,
        RowTemplate = { Height = 40 }, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, AccessibleName = "Lista de registros"
    };
}
