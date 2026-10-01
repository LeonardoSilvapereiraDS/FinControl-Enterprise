using System.Globalization;
using System.Windows.Forms.DataVisualization.Charting;
using FinControl.Application.Dashboard;
using FinControl.Infrastructure.Persistence;
using FinControl.WinForms.Components;
using FinControl.WinForms.Design;

namespace FinControl.WinForms;

public sealed class DashboardControl : UserControl
{
    private readonly IDashboardService _dashboardService;
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("pt-BR");
    private readonly SummaryCard _saldoCard = new();
    private readonly SummaryCard _receitasCard = new();
    private readonly SummaryCard _despesasCard = new();
    private readonly SummaryCard _economiaCard = new();
    private readonly ChartPanel _linhaPanel = new("Receitas x Despesas");
    private readonly ChartPanel _categoriaPanel = new("Categorias");
    private readonly ChartPanel _barrasPanel = new("Gastos mensais");
    private readonly ChartPanel _ultimasPanel = new("Ultimas transacoes");
    private readonly BudgetProgressCard _orcamentoCard = new();
    private readonly Label _stateLabel = new();

    public event EventHandler? NovaTransacaoSolicitada;

    public DashboardControl(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        AutoScaleMode = AutoScaleMode.None;
        AutoSize = false;
        Margin = Padding.Empty;

        ConfigurarTela();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        await AtualizarAsync();
    }

    public async Task AtualizarAsync()
    {
        try
        {
            MostrarEstado("Carregando dashboard...");

            var dashboard = await _dashboardService.ObterDashboardAsync(
                BancoDadosInicializador.UsuarioPadraoId,
                DateTime.Today);

            AtualizarCards(dashboard);
            AtualizarGraficos(dashboard);
            AtualizarUltimasTransacoes(dashboard);
            MostrarEstado(dashboard.PossuiTransacoes
                ? $"Atualizado em {DateTime.Now:dd/MM/yyyy HH:mm}"
                : "Sem transacoes cadastradas neste momento.");
        }
        catch (Exception ex)
        {
            MostrarEstado("Erro ao carregar dashboard.");
            MessageBox.Show(
                ex.Message,
                "FinControl",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void ConfigurarTela()
    {
        BackColor = AppTheme.Background;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = AppTheme.Background
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 126));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        root.Controls.Add(CriarCardsResumo(), 0, 0);
        root.Controls.Add(CriarAreaSuperior(), 0, 1);
        root.Controls.Add(CriarAreaInferior(), 0, 2);

        _stateLabel.Dock = DockStyle.Fill;
        _stateLabel.ForeColor = AppTheme.MutedText;
        _stateLabel.Font = AppTheme.LabelFont;
        _stateLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_stateLabel, 0, 3);

        Controls.Add(root);
    }

    private Control CriarCardsResumo()
    {
        var cards = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        for (var i = 0; i < 4; i++)
        {
            cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        cards.Controls.Add(_saldoCard, 0, 0);
        cards.Controls.Add(_receitasCard, 1, 0);
        cards.Controls.Add(_despesasCard, 2, 0);
        cards.Controls.Add(_economiaCard, 3, 0);

        return cards;
    }

    private Control CriarAreaSuperior()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));

        grid.Controls.Add(_linhaPanel, 0, 0);
        grid.Controls.Add(_orcamentoCard, 1, 0);

        return grid;
    }

    private Control CriarAreaInferior()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

        grid.Controls.Add(_categoriaPanel, 0, 0);
        grid.Controls.Add(_barrasPanel, 1, 0);
        grid.Controls.Add(_ultimasPanel, 2, 0);

        return grid;
    }

    private void AtualizarCards(DashboardDto dashboard)
    {
        _saldoCard.Atualizar(
            "Saldo geral",
            dashboard.SaldoGeral.ToString("C", _culture),
            "R$",
            AppTheme.Blue,
            "Contas e pagamentos");

        _receitasCard.Atualizar(
            "Receitas mes",
            dashboard.TotalReceitasMes.ToString("C", _culture),
            "+",
            AppTheme.Green,
            FormatarVariacao(dashboard.PercentualVariacaoReceitas));

        _despesasCard.Atualizar(
            "Despesas mes",
            dashboard.TotalDespesasMes.ToString("C", _culture),
            "-",
            AppTheme.Red,
            FormatarVariacao(dashboard.PercentualVariacaoDespesas));

        _economiaCard.Atualizar(
            "Economia",
            dashboard.EconomiaMes.ToString("C", _culture),
            "%",
            dashboard.EconomiaMes >= 0 ? AppTheme.Cyan : AppTheme.Warning,
            dashboard.EconomiaMes >= 0 ? "Resultado positivo" : "Resultado negativo");
    }

    private void AtualizarGraficos(DashboardDto dashboard)
    {
        _linhaPanel.SetContent(CriarGraficoReceitasDespesas(dashboard));
        _categoriaPanel.SetContent(CriarGraficoCategorias(dashboard));
        _barrasPanel.SetContent(CriarGraficoBarras(dashboard));
        _orcamentoCard.Atualizar(
            dashboard.OrcamentoTotal,
            dashboard.OrcamentoUtilizado,
            dashboard.OrcamentoDisponivel,
            dashboard.PercentualOrcamentoUtilizado);
    }

    private Chart CriarGraficoReceitasDespesas(DashboardDto dashboard)
    {
        var chart = CriarChartBase();
        var area = chart.ChartAreas[0];

        area.AxisX.MajorGrid.Enabled = false;
        area.AxisY.LabelStyle.Format = "C0";

        var receitas = CriarSerieLinha("Receitas", AppTheme.Green);
        var despesas = CriarSerieLinha("Despesas", AppTheme.Red);

        foreach (var item in dashboard.ReceitasPorMes)
        {
            var index = receitas.Points.AddXY(item.Mes.ToString("MMM", _culture), item.Valor);
            receitas.Points[index].ToolTip = item.Valor.ToString("C", _culture);
        }

        foreach (var item in dashboard.DespesasPorMes)
        {
            var index = despesas.Points.AddXY(item.Mes.ToString("MMM", _culture), item.Valor);
            despesas.Points[index].ToolTip = item.Valor.ToString("C", _culture);
        }

        chart.Series.Add(receitas);
        chart.Series.Add(despesas);

        return chart;
    }

    private Chart CriarGraficoCategorias(DashboardDto dashboard)
    {
        var chart = CriarChartBase();
        chart.ChartAreas[0].AxisX.Enabled = AxisEnabled.False;
        chart.ChartAreas[0].AxisY.Enabled = AxisEnabled.False;

        var serie = new Series("Categorias")
        {
            ChartType = SeriesChartType.Doughnut,
            Font = AppTheme.LabelFont,
            LabelForeColor = AppTheme.Text
        };

        serie["DoughnutRadius"] = "58";

        if (dashboard.DespesasPorCategoria.Count == 0)
        {
            serie.Points.AddXY("Sem despesas", 1);
            serie.Points[0].Color = AppTheme.PanelAlt;
            serie.Points[0].Label = "Sem dados";
        }
        else
        {
            var colors = new[] { AppTheme.Cyan, AppTheme.Purple, AppTheme.Magenta, AppTheme.Blue, AppTheme.Warning };

            for (var i = 0; i < dashboard.DespesasPorCategoria.Count; i++)
            {
                var item = dashboard.DespesasPorCategoria[i];
                var pointIndex = serie.Points.AddXY(item.Categoria, item.Valor);
                var point = serie.Points[pointIndex];

                point.Color = colors[i % colors.Length];
                point.LegendText = $"{item.Categoria} ({item.Percentual:N0}%)";
                point.Label = $"{item.Percentual:N0}%";
                point.ToolTip = $"{item.Categoria}: {item.Valor.ToString("C", _culture)}";
            }
        }

        chart.Series.Add(serie);

        return chart;
    }

    private Chart CriarGraficoBarras(DashboardDto dashboard)
    {
        var chart = CriarChartBase();
        var area = chart.ChartAreas[0];

        area.AxisX.MajorGrid.Enabled = false;
        area.AxisY.LabelStyle.Format = "C0";

        var serie = new Series("Gastos")
        {
            ChartType = SeriesChartType.Column,
            Color = AppTheme.Cyan,
            BorderWidth = 0,
            IsValueShownAsLabel = false
        };

        foreach (var item in dashboard.DespesasPorMes)
        {
            var pointIndex = serie.Points.AddXY(item.Mes.ToString("MMM", _culture), item.Valor);
            serie.Points[pointIndex].ToolTip = item.Valor.ToString("C", _culture);
        }

        chart.Series.Add(serie);

        return chart;
    }

    private void AtualizarUltimasTransacoes(DashboardDto dashboard)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            AutoScroll = true
        };

        if (dashboard.UltimasTransacoes.Count == 0)
        {
            var empty = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };

            empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            empty.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            empty.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var button = new Button
            {
                Dock = DockStyle.Fill,
                Text = "Nova transacao",
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.Purple,
                ForeColor = AppTheme.Text,
                Font = AppTheme.SectionFont,
                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => NovaTransacaoSolicitada?.Invoke(this, EventArgs.Empty);

            empty.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                Text = "Nenhuma transacao.",
                ForeColor = AppTheme.MutedText,
                Font = AppTheme.SectionFont,
                TextAlign = ContentAlignment.BottomCenter
            }, 0, 0);
            empty.Controls.Add(button, 0, 1);

            panel.Controls.Add(empty);
        }
        else
        {
            var button = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                Text = "Ver todas as transacoes",
                FlatStyle = FlatStyle.Flat,
                BackColor = AppTheme.PanelAlt,
                ForeColor = AppTheme.Text,
                Cursor = Cursors.Hand
            };

            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => NovaTransacaoSolicitada?.Invoke(this, EventArgs.Empty);

            panel.Controls.Add(button);

            foreach (var transacao in dashboard.UltimasTransacoes.Reverse())
            {
                panel.Controls.Add(new TransactionItem(transacao));
            }
        }

        _ultimasPanel.SetContent(panel);
    }

    private static Chart CriarChartBase()
    {
        var chart = new Chart
        {
            BackColor = AppTheme.Panel,
            ForeColor = AppTheme.Text,
            Palette = ChartColorPalette.None,
            BorderlineWidth = 0
        };

        var area = new ChartArea("Principal")
        {
            BackColor = AppTheme.Panel
        };

        area.AxisX.LabelStyle.ForeColor = AppTheme.MutedText;
        area.AxisY.LabelStyle.ForeColor = AppTheme.MutedText;
        area.AxisX.LineColor = AppTheme.Border;
        area.AxisY.LineColor = AppTheme.Border;
        area.AxisX.MajorGrid.LineColor = AppTheme.Border;
        area.AxisY.MajorGrid.LineColor = AppTheme.Border;
        area.AxisX.MajorTickMark.LineColor = AppTheme.Border;
        area.AxisY.MajorTickMark.LineColor = AppTheme.Border;

        chart.ChartAreas.Add(area);
        chart.Legends.Add(new Legend
        {
            BackColor = AppTheme.Panel,
            ForeColor = AppTheme.MutedText,
            Docking = Docking.Top,
            Alignment = StringAlignment.Far
        });

        return chart;
    }

    private static Series CriarSerieLinha(string nome, Color cor)
    {
        return new Series(nome)
        {
            ChartType = SeriesChartType.Spline,
            Color = cor,
            BorderWidth = 3,
            MarkerStyle = MarkerStyle.Circle,
            MarkerSize = 7,
            MarkerColor = cor,
            XValueType = ChartValueType.String
        };
    }

    private static string FormatarVariacao(decimal? variacao)
    {
        return variacao is null
            ? "Sem comparativo"
            : $"{variacao:+0.##;-0.##;0}% vs mes anterior";
    }

    private void MostrarEstado(string mensagem)
    {
        _stateLabel.Text = mensagem;
    }
}
