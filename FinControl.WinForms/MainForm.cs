using System.Globalization;
using FinControl.WinForms.Components;
using FinControl.WinForms.Design;

namespace FinControl.WinForms;

public sealed class MainForm : Form
{
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("pt-BR");
    private readonly DashboardControl _dashboardControl;
    private readonly TransacoesControl _transacoesControl;
    private readonly CategoriasControl _categoriasControl;
    private readonly ContasControl _contasControl;
    private readonly Panel _contentHost = new();
    private readonly PageHeader _pageHeader = new();
    private readonly Button _novaTransacaoButton = CriarHeaderButton("+ Nova", true);
    private readonly List<MenuButton> _menuButtons = [];

    public MainForm(
        DashboardControl dashboardControl,
        TransacoesControl transacoesControl,
        CategoriasControl categoriasControl,
        ContasControl contasControl)
    {
        _dashboardControl = dashboardControl;
        _transacoesControl = transacoesControl;
        _categoriasControl = categoriasControl;
        _contasControl = contasControl;

        AutoScaleMode = AutoScaleMode.None;
        Text = "FinControl";
        MinimumSize = new Size(1280, 720);
        Size = new Size(1280, 720);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = AppTheme.Background;
        Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        ConfigurarTela();
        ConfigurarAtualizacaoDashboard();
        Navegar("Dashboard");
    }

    private void ConfigurarTela()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = AppTheme.Background
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.Controls.Add(CriarMenuLateral(), 0, 0);
        root.Controls.Add(CriarAreaPrincipal(), 1, 0);

        Controls.Add(root);
    }

    private Control CriarMenuLateral()
    {
        var sidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Sidebar,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(14, 18, 14, 14)
        };

        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));

        sidebar.Controls.Add(CriarLogo(), 0, 0);
        sidebar.Controls.Add(CriarMenu(), 0, 1);

        var sair = CriarMenuButton("Sair", "Sair");
        sair.Click += (_, _) => Close();
        sidebar.Controls.Add(sair, 0, 2);

        return sidebar;
    }

    private static Control CriarLogo()
    {
        var logo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        logo.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        logo.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        logo.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "FinControl",
            Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = AppTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        logo.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Financeiro pessoal",
            Font = AppTheme.LabelFont,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 1);

        return logo;
    }

    private Control CriarMenu()
    {
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent
        };

        AdicionarMenu(flow, "Dashboard", "Dashboard");
        AdicionarMenu(flow, "Transacoes", "Transacoes");
        AdicionarMenu(flow, "Receitas", "Receitas");
        AdicionarMenu(flow, "Despesas", "Despesas");
        AdicionarMenu(flow, "Categorias", "Categorias");
        AdicionarMenu(flow, "Contas", "Contas");
        AdicionarMenu(flow, "Orcamentos", "Orcamentos");
        AdicionarMenu(flow, "Metas", "Metas financeiras");
        AdicionarMenu(flow, "Relatorios", "Relatorios");
        AdicionarMenu(flow, "Configuracoes", "Configuracoes");

        return flow;
    }

    private void AdicionarMenu(FlowLayoutPanel flow, string key, string texto)
    {
        var button = CriarMenuButton(key, texto);
        button.Click += (_, _) => Navegar(key);

        _menuButtons.Add(button);
        flow.Controls.Add(button);
    }

    private static MenuButton CriarMenuButton(string key, string texto)
    {
        return new MenuButton(key, texto)
        {
            Width = 212
        };
    }

    private Control CriarAreaPrincipal()
    {
        var area = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = AppTheme.Background,
            Padding = new Padding(16)
        };

        area.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        area.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _contentHost.Dock = DockStyle.Fill;
        _contentHost.BackColor = Color.Transparent;

        area.Controls.Add(_pageHeader, 0, 0);
        area.Controls.Add(_contentHost, 0, 1);

        return area;
    }

    private void Navegar(string key)
    {
        foreach (var button in _menuButtons)
        {
            button.Selected = button.MenuKey == key;
        }

        Control control = key switch
        {
            "Dashboard" => _dashboardControl,
            "Transacoes" or "Receitas" or "Despesas" => _transacoesControl,
            "Categorias" => _categoriasControl,
            "Contas" => _contasControl,
            "Orcamentos" => CriarPlaceholder("Orcamentos"),
            "Metas" => CriarPlaceholder("Metas financeiras"),
            "Relatorios" => CriarPlaceholder("Relatorios"),
            "Configuracoes" => CriarPlaceholder("Configuracoes"),
            _ => _dashboardControl
        };

        if (key is "Transacoes" or "Receitas" or "Despesas")
            _transacoesControl.DefinirFiltroTipo(key == "Receitas" ? FinControl.Domain.Enums.TipoTransacao.Receita : key == "Despesas" ? FinControl.Domain.Enums.TipoTransacao.Despesa : null);
        AtualizarCabecalho(key);
        _contentHost.Controls.Clear();
        control.Dock = DockStyle.Fill;
        _contentHost.Controls.Add(control);
    }

    private static Control CriarPlaceholder(string titulo)
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Panel,
            CornerRadius = 18
        };

        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = $"{titulo}\nEm desenvolvimento",
            Font = AppTheme.TitleFont,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.MiddleCenter
        });

        return panel;
    }

    private void ConfigurarAtualizacaoDashboard()
    {
        _dashboardControl.NovaTransacaoSolicitada += (_, _) => Navegar("Transacoes");
        _novaTransacaoButton.Click += async (_, _) => await _transacoesControl.AbrirNovaTransacaoAsync();
        _categoriasControl.DadosAlterados += async (_, _) => await _dashboardControl.AtualizarAsync();
        _contasControl.DadosAlterados += async (_, _) => await _dashboardControl.AtualizarAsync();
        _transacoesControl.DadosAlterados += async (_, _) => await _dashboardControl.AtualizarAsync();
    }

    private void AtualizarCabecalho(string key)
    {
        var titulo = key switch
        {
            "Dashboard" => "Dashboard",
            "Transacoes" => "Transacoes",
            "Receitas" => "Receitas",
            "Despesas" => "Despesas",
            "Categorias" => "Categorias",
            "Contas" => "Contas",
            "Orcamentos" => "Orcamentos",
            "Metas" => "Metas financeiras",
            "Relatorios" => "Relatorios",
            "Configuracoes" => "Configuracoes",
            _ => "Dashboard"
        };

        var subtitulo = $"Usuario Local | {DateTime.Today.ToString("MMMM yyyy", _culture)}";
        _pageHeader.SetText(titulo, subtitulo);
        _pageHeader.SetActions(_novaTransacaoButton);
    }

    private static Button CriarHeaderButton(string text, bool primary)
    {
        var button = new Button
        {
            AutoSize = false,
            Width = 164,
            Height = 38,
            Text = text
        };

        if (primary)
        {
            ControlStyler.StylePrimaryButton(button);
        }
        else
        {
            ControlStyler.StyleSecondaryButton(button);
        }

        return button;
    }

}
