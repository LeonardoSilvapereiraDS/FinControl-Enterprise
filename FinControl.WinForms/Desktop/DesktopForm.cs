using System.Text.Json.Nodes;
using Microsoft.Win32;

namespace FinControl.WinForms.Desktop;

internal sealed partial class DesktopForm : Form
{
    private readonly FinanceClient client = new();
    private readonly Panel content = new() { Dock = DockStyle.Fill, Padding = new Padding(28, 20, 28, 20) };
    private readonly FlowLayoutPanel sidebar = new() { Dock = DockStyle.Left, Width = 218, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(16, 26, 12, 18) };
    private readonly FlowLayoutPanel topbar = new() { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(24, 10, 12, 8), WrapContents = true };
    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(24, 6, 0, 0), Text = "Preparando seu espaço financeiro…" };
    private readonly ComboBox workspaces = new() { Width = 245, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Espaço financeiro", Margin = new Padding(4, 6, 16, 4) };
    private readonly DateTimePicker month = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "MMMM yyyy", ShowUpDown = true, Width = 170, AccessibleName = "Mês de referência", Margin = new Padding(4, 6, 12, 4) };
    private readonly ComboBox theme = new() { Width = 102, DropDownStyle = ComboBoxStyle.DropDownList, AccessibleName = "Tema", Margin = new Padding(4, 6, 12, 4) };
    private JsonNode state = new JsonObject();
    private string page = "Visão geral";
    private string userName = "";
    private bool busy, loading, ready, closing, stopped;
    private DataGridView? grid;
    private TextBox? search;
    private ComboBox? payment;
    private bool Editable => state.Text("role") != "Leitor";
    private bool Admin => state.Text("role") == "Administrador";
    private static readonly string[] Pages = ["Visão geral", "Lançamentos", "Receitas", "Despesas", "Contas", "Categorias", "Orçamentos", "Metas", "Recorrências", "Relatórios", "Importar extrato", "Configurações"];

    public DesktopForm(bool smoke = false)
    {
        using var iconStream = typeof(DesktopForm).Assembly.GetManifestResourceStream("FinControl.WinForms.Assets.App.ico");
        if (iconStream != null) Icon = new Icon(iconStream);
        Text = "FinControl Enterprise • Windows"; Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(1280, 800); MinimumSize = new Size(980, 640);
        StartPosition = FormStartPosition.CenterScreen; DoubleBuffered = true;
        Controls.Add(content); Controls.Add(status); Controls.Add(topbar); Controls.Add(sidebar);
        var brand = DesktopUi.Label("FinControl", 23, true); brand.Width = 180; sidebar.Controls.Add(brand);
        sidebar.Controls.Add(DesktopUi.Label("ENTERPRISE  /  WINDOWS", 8));
        sidebar.Controls.Add(new Panel { Height = 18, Width = 170 });
        foreach (var name in Pages)
        {
            var button = DesktopUi.Button(name, () => Execute(async () => { page = name; await RefreshState(); }));
            button.Width = 180; button.Height = 40; button.AutoSize = false; button.TextAlign = ContentAlignment.MiddleLeft;
            sidebar.Controls.Add(button);
        }
        topbar.Controls.Add(workspaces); topbar.Controls.Add(month);
        theme.Items.AddRange(["Sistema", "Claro", "Escuro"]); theme.SelectedItem = DesktopTheme.Mode;
        topbar.Controls.Add(theme);
        topbar.Controls.Add(DesktopUi.Button("Atualizar", () => Execute(RefreshState)));
        topbar.Controls.Add(DesktopUi.Button("Sair", () => Execute(async () => { await client.Post("/api/auth/logout"); ShowWelcome(); })));
        workspaces.SelectedIndexChanged += async (_, _) =>
        {
            if (loading || !ready) return;
            await Execute(async () => { client.WorkspaceId = Convert.ToInt32(((Option)workspaces.SelectedItem!).Value); await RefreshState(); });
        };
        month.ValueChanged += async (_, _) => { if (ready && !loading) await Execute(RefreshState); };
        theme.SelectedIndexChanged += (_, _) => { DesktopTheme.Set(theme.Text); ApplyTheme(); };
        Shown += async (_, _) =>
        {
            ApplyTheme(); sidebar.Enabled = topbar.Enabled = false;
            try { await client.StartAsync(); if (smoke) await RunSmoke(); else ShowWelcome(); }
            catch (Exception ex) { if (smoke) { WriteSmokeFailure(ex); Environment.ExitCode = 1; } else DesktopUi.Error(this, ex); Close(); }
        };
        FormClosing += OnClosing;
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
    }
    private void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (DesktopTheme.Mode == "Sistema" && IsHandleCreated && !IsDisposed)
            BeginInvoke((Action)(() => { DesktopTheme.Load(); ApplyTheme(); }));
    }
    private async void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (stopped) return;
        e.Cancel = true; if (closing) return;
        closing = true; Enabled = false;
        SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
        try { await client.DisposeAsync(); } catch (Exception) { /* Closing must not strand the window. */ }
        stopped = true; Close();
    }
    private void ApplyTheme()
    {
        DesktopTheme.Apply(this); sidebar.BackColor = DesktopTheme.Sidebar;
        foreach (Control control in sidebar.Controls)
        {
            control.BackColor = DesktopTheme.Sidebar; control.ForeColor = Color.FromArgb(198, 213, 232);
            if (control is Button button)
            {
                button.FlatAppearance.BorderSize = 0;
                if (button.Text == page) { button.BackColor = Color.FromArgb(30, 65, 78); button.ForeColor = Color.FromArgb(94, 234, 212); }
            }
        }
        status.ForeColor = DesktopTheme.Muted;
    }
    private async Task Execute(Func<Task> action)
    {
        if (busy || closing) return;
        busy = true; UseWaitCursor = true; sidebar.Enabled = topbar.Enabled = content.Enabled = false;
        try { await action(); }
        catch (Exception ex) { DesktopUi.Error(this, ex); status.Text = "Operação não concluída. Confira a mensagem e tente novamente."; }
        finally
        {
            busy = false; UseWaitCursor = false;
            if (!IsDisposed) { sidebar.Enabled = topbar.Enabled = ready; content.Enabled = true; }
        }
    }
    private void ShowWelcome()
    {
        ready = false; sidebar.Enabled = topbar.Enabled = false;
        DesktopUi.Clear(content); grid = null; state = new JsonObject();
        var welcome = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(32, 60, 24, 24), AutoScroll = true };
        welcome.Controls.Add(DesktopUi.Label("Suas finanças, com clareza.", 28, true));
        welcome.Controls.Add(DesktopUi.Label("FinControl Enterprise", 15, true));
        welcome.Controls.Add(DesktopUi.Label("Pessoal, MEI ou empresa. Escolha um espaço e acompanhe cada movimento."));
        welcome.Controls.Add(new Panel { Height = 26 });
        welcome.Controls.Add(DesktopUi.Button("Entrar na minha conta", () => Authenticate(false), true));
        welcome.Controls.Add(DesktopUi.Button("Criar uma conta", () => Authenticate(true)));
        welcome.Controls.Add(DesktopUi.Label("Se você já usou a versão web neste computador, entre com o mesmo cadastro."));
        welcome.Controls.Add(DesktopUi.Label("Seus dados ficam neste computador. Não é preciso contratar hospedagem."));
        content.Controls.Add(welcome); status.Text = "Aplicativo Windows • Acesso local • Dados preservados"; ApplyTheme();
    }
    private async Task Authenticate(bool register)
    {
        var session = await client.Get("/api/session");
        using var form = new EditorDialog(register ? "Criar sua conta" : "Bem-vindo de volta", register ? "Use uma senha de 10 a 128 caracteres." : "Informe o cadastro usado neste computador.");
        if (register) form.TextField("nome", "Seu nome");
        form.TextField("email", "E-mail").TextField("senha", "Senha", password: true);
        if (register)
        {
            form.TextField("confirmacao", "Repita a senha", password: true)
                .TextField("espaco", "Nome do primeiro espaço", "Minhas finanças")
                .Select("perfil", "Perfil", Profiles());
            if (session.Flag("requiresSetup")) form.Note("Foram encontrados dados da versão antiga. Eles serão vinculados à primeira conta em espaços chamados Dados anteriores.");
        }
        form.OnSave(async values =>
        {
            if (register)
            {
                if (values.Text("senha") != values.Text("confirmacao")) throw new InvalidOperationException("As senhas não coincidem.");
                values.Remove("confirmacao"); values["codigoInicial"] = client.SetupCode;
            }
            await client.Post(register ? "/api/auth/register" : "/api/auth/login", values);
        }, register ? "Criar conta" : "Entrar");
        if (form.ShowDialog(this) == DialogResult.OK) await Execute(LoadSession);
    }
    private static Option[] Profiles() => [new("Pessoal", "Pessoal"), new("MEI", "MEI / microempresa"), new("Empresa", "Empresa")];
    private async Task LoadSession()
    {
        var session = await client.Get("/api/session"); userName = session["user"].Text("nome");
        loading = true; workspaces.Items.Clear();
        foreach (var space in session.Items("workspaces")) workspaces.Items.Add(new Option(space.Int("id"), $"{space.Text("nome")} · {space.Text("perfil")}"));
        if (workspaces.Items.Count == 0) throw new InvalidOperationException("Nenhum espaço disponível para esta conta.");
        workspaces.SelectedIndex = 0;
        for (var i = 0; i < workspaces.Items.Count; i++) if (Equals(((Option)workspaces.Items[i]!).Value, client.WorkspaceId)) workspaces.SelectedIndex = i;
        client.WorkspaceId = Convert.ToInt32(((Option)workspaces.SelectedItem!).Value);
        loading = false; ready = true; await RefreshState();
    }
    private async Task RefreshState()
    {
        if (!ready) return;
        state = await client.Get(client.Path($"state?month={month.Value:yyyy-MM}"));
        RenderPage(); status.Text = $"{userName}   •   {state.Text("role")}   •   Atualizado às {DateTime.Now:HH:mm}   •   Dados locais";
    }
    private FlowLayoutPanel PageHeader(string title, string description)
    {
        DesktopUi.Clear(content); grid = null; search = null; payment = null;
        var header = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        header.Controls.Add(DesktopUi.Label(title, 24, true));
        var sub = DesktopUi.Label(description); sub.Tag = "muted"; header.Controls.Add(sub);
        var toolbar = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0, 10, 0, 14), MaximumSize = new Size(Math.Max(650, content.Width - 60), 0) };
        header.Controls.Add(toolbar); content.Controls.Add(header); return toolbar;
    }
    private void AddAction(FlowLayoutPanel bar, string caption, Func<Task> action, bool write = false, bool admin = false, bool primary = false)
    {
        var button = DesktopUi.Button(caption, () => Execute(action), primary); button.Enabled = (!write || Editable) && (!admin || Admin); bar.Controls.Add(button);
    }
    private void RenderPage()
    {
        if (page == "Visão geral") RenderDashboard();
        else if (page is "Lançamentos" or "Receitas" or "Despesas") RenderTransactions();
        else if (page == "Relatórios") RenderReports();
        else if (page == "Importar extrato") RenderImport();
        else if (page == "Configurações") RenderSettings();
        else RenderRecords();
        ApplyTheme();
    }
    private void AttachGrid(string[] columns, IEnumerable<(JsonNode Node, object[] Cells)> rows)
    {
        grid = DesktopUi.Grid();
        for (var i = 0; i < columns.Length; i++) grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "c" + i, HeaderText = columns[i], MinimumWidth = i == 0 ? 150 : 95, SortMode = DataGridViewColumnSortMode.Automatic });
        foreach (var (node, cells) in rows) { var index = grid.Rows.Add(cells); grid.Rows[index].Tag = node; }
        content.Controls.Add(grid); grid.BringToFront();
        if (grid.Rows.Count == 0)
        {
            var empty = new Label { Text = "Nenhum registro neste período. Use os botões acima para começar.", Dock = DockStyle.Bottom, Height = 48, TextAlign = ContentAlignment.MiddleCenter };
            content.Controls.Add(empty); empty.BringToFront();
        }
    }
    private JsonNode Selected() => grid?.CurrentRow?.Tag as JsonNode ?? throw new InvalidOperationException("Selecione um registro na tabela primeiro.");
    private IEnumerable<JsonNode> Transactions(bool currentMonth = true) => state.Items("transactions").Where(t => !currentMonth || (t.Date("data").Month == month.Value.Month && t.Date("data").Year == month.Value.Year));
    private static string Currency(decimal value) => value.ToString("C2");
}
