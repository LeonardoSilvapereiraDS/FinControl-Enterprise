using System.Drawing.Drawing2D;
using System.Text;
using System.Text.Json.Nodes;

namespace FinControl.WinForms.Desktop;

internal sealed partial class DesktopForm
{
    private void RenderDashboard()
    {
        var bar = PageHeader("Seu financeiro, com clareza.", $"{state["workspace"].Text("nome")} • {month.Value:MMMM yyyy}");
        AddAction(bar, "Novo lançamento", () => EditTransaction(), write: true, primary: true);
        AddAction(bar, "Ver pendências", () => { page = "Lançamentos"; RenderPage(); payment!.SelectedIndex = 2; return Task.CompletedTask; });
        var body = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(0, 0, 12, 0) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 136, ColumnCount = 4, Margin = new Padding(0, 0, 0, 16) };
        for (var i = 0; i < 4; i++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        var dashboard = state["dashboard"];
        cards.Controls.Add(new MetricCard("Saldo consolidado", dashboard.Money("saldoGeral"), "Todas as contas • pagos", false));
        cards.Controls.Add(new MetricCard("Receitas do mês", dashboard.Money("totalReceitasMes"), "Pagamentos confirmados", false));
        cards.Controls.Add(new MetricCard("Despesas do mês", dashboard.Money("totalDespesasMes"), "Pagamentos confirmados", true));
        cards.Controls.Add(new MetricCard("Resultado do mês", dashboard.Money("economiaMes"), "Receitas menos despesas", dashboard.Money("economiaMes") < 0));
        layout.Controls.Add(cards);
        var pending = Transactions().Where(t => !t.Flag("pago")).ToArray();
        var note = DesktopUi.Label($"A receber: {Currency(pending.Where(t => t.Int("tipo") == 1).Sum(t => t.Money("valor")))}     •     A pagar: {Currency(pending.Where(t => t.Int("tipo") == 2).Sum(t => t.Money("valor")))}     •     Pendências do mês: {pending.Length}");
        note.Margin = new Padding(4, 0, 0, 18); layout.Controls.Add(note);
        layout.Controls.Add(DesktopUi.Label("Fluxo financeiro • últimos 6 meses", 14, true));
        var chart = new CashChart { Dock = DockStyle.Top, Height = 230, Margin = new Padding(0, 0, 0, 16) };
        var start = new DateTime(month.Value.Year, month.Value.Month, 1);
        chart.Points = Enumerable.Range(-5, 6).Select(offset =>
        {
            var date = start.AddMonths(offset); var tx = Transactions(false).Where(t => t.Flag("pago") && t.Date("data").Month == date.Month && t.Date("data").Year == date.Year).ToArray();
            return (date.ToString("MMM/yy"), tx.Where(t => t.Int("tipo") == 1).Sum(t => t.Money("valor")), tx.Where(t => t.Int("tipo") == 2).Sum(t => t.Money("valor")));
        }).ToArray();
        layout.Controls.Add(chart); layout.Controls.Add(DesktopUi.Label("Últimos lançamentos do mês", 14, true));
        var recent = DesktopUi.Grid(); recent.Dock = DockStyle.Top; recent.Height = 245;
        foreach (var heading in new[] { "Descrição", "Data", "Valor", "Situação" }) recent.Columns.Add(heading, heading);
        foreach (var t in Transactions().OrderByDescending(t => t.Date("data")).ThenByDescending(t => t.Int("id")).Take(8))
            recent.Rows.Add(t.Text("descricao"), t.Date("data").ToString("dd/MM"), (t.Int("tipo") == 1 ? "+ " : "− ") + Currency(t.Money("valor")), t.Flag("pago") ? "Pago" : "Pendente");
        if (recent.Rows.Count == 0) layout.Controls.Add(DesktopUi.Label("Seu painel está pronto. Cadastre o primeiro lançamento para começar."));
        else layout.Controls.Add(recent);
        body.Controls.Add(layout); content.Controls.Add(body); body.BringToFront();
    }
    private void RenderReports()
    {
        var bar = PageHeader("Relatórios", "Resumo por categoria no mês selecionado. Valores pagos e pendentes separados.");
        AddAction(bar, "Exportar CSV", ExportGrid);
        var rows = Transactions().GroupBy(t => (Id: t.Int("categoriaId"), Name: t.Text("categoriaNome"), Type: t.Int("tipo")))
            .OrderBy(g => g.Key.Type).ThenBy(g => g.Key.Name).Select(g => ((JsonNode)new JsonObject(), new object[]
            { g.Key.Name, g.Key.Type == 1 ? "Receita" : "Despesa", g.Count(), Currency(g.Where(t => t.Flag("pago")).Sum(t => t.Money("valor"))), Currency(g.Where(t => !t.Flag("pago")).Sum(t => t.Money("valor"))), Currency(g.Sum(t => t.Money("valor"))) }));
        AttachGrid(["Categoria", "Tipo", "Lançamentos", "Pago", "Pendente", "Total previsto"], rows);
    }
    private async Task ExportGrid()
    {
        if (grid == null || grid.Rows.Count == 0) throw new InvalidOperationException("Não há registros para exportar.");
        using var dialog = new SaveFileDialog { Filter = "Planilha CSV (*.csv)|*.csv", FileName = $"FinControl-{page.Replace(' ', '-')}-{month.Value:yyyy-MM}.csv", OverwritePrompt = true };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        static string Cell(object? value)
        {
            var text = value?.ToString() ?? "";
            // Protect exported text against spreadsheet formula execution.
            if (text.TrimStart().StartsWith('=') || text.TrimStart().StartsWith('+') || text.TrimStart().StartsWith('-') || text.TrimStart().StartsWith('@')) text = "'" + text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        var lines = new List<string> { string.Join(';', grid.Columns.Cast<DataGridViewColumn>().Select(c => Cell(c.HeaderText))) };
        lines.AddRange(grid.Rows.Cast<DataGridViewRow>().Where(r => r.Visible).Select(r => string.Join(';', r.Cells.Cast<DataGridViewCell>().Select(c => Cell(c.Value)))));
        await File.WriteAllLinesAsync(dialog.FileName, lines, new UTF8Encoding(true));
        status.Text = "CSV exportado com sucesso.";
    }
}

internal sealed class MetricCard(string title, decimal amount, string caption, bool negative) : Control
{
    public MetricCard() : this("", 0, "", false) { }
    protected override void OnCreateControl() { base.OnCreateControl(); Dock = DockStyle.Fill; Margin = new Padding(0, 0, 12, 0); DoubleBuffered = true; AccessibleName = $"{title}: {amount:C2}"; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.Clear(DesktopTheme.Surface);
        using var pen = new Pen(DesktopTheme.Border); g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        using var accent = new SolidBrush(negative ? DesktopTheme.Negative : DesktopTheme.Accent); g.FillRectangle(accent, 0, 0, Width, 4);
        using var label = new Font("Segoe UI", 10); using var value = new Font("Segoe UI", Width < 210 ? 17 : 22, FontStyle.Bold); using var small = new Font("Segoe UI", 8);
        TextRenderer.DrawText(g, title, label, new Rectangle(16, 17, Width - 28, 24), DesktopTheme.Muted, TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, amount.ToString("C2"), value, new Rectangle(12, 45, Width - 24, 43), DesktopTheme.Text, TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(g, caption, small, new Rectangle(16, 101, Width - 28, 20), DesktopTheme.Muted, TextFormatFlags.EndEllipsis);
    }
}

internal sealed class CashChart : Control
{
    public (string Label, decimal Income, decimal Expense)[] Points { get; set; } = [];
    public CashChart() { DoubleBuffered = true; AccessibleName = "Gráfico de receitas e despesas pagas nos últimos seis meses"; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.Clear(DesktopTheme.Surface); g.SmoothingMode = SmoothingMode.AntiAlias;
        using var font = new Font("Segoe UI", 9); using var incomeBrush = new SolidBrush(DesktopTheme.Accent); using var expenseBrush = new SolidBrush(DesktopTheme.Negative);
        using var line = new Pen(DesktopTheme.Border);
        var max = Points.Select(p => Math.Max(p.Income, p.Expense)).DefaultIfEmpty(0).Max();
        if (max == 0) { TextRenderer.DrawText(g, "Os valores aparecerão aqui após seus primeiros pagamentos.", font, ClientRectangle, DesktopTheme.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); return; }
        int bottom = Height - 36, top = 45, left = 82, plot = Math.Max(60, Width - left - 20);
        for (int i = 0; i <= 3; i++)
        {
            int y = bottom - (bottom - top) * i / 3; g.DrawLine(line, left, y, Width - 15, y);
            TextRenderer.DrawText(g, (max * i / 3).ToString("N0"), font, new Rectangle(0, y - 9, 76, 20), DesktopTheme.Muted, TextFormatFlags.Right);
        }
        for (int i = 0; i < Points.Length; i++)
        {
            var p = Points[i]; int step = plot / Points.Length, x = left + step * i + step / 2, width = Math.Max(6, Math.Min(24, step / 4));
            int a = (int)(p.Income / max * (bottom - top)), b = (int)(p.Expense / max * (bottom - top));
            g.FillRectangle(incomeBrush, x - width - 2, bottom - a, width, a); g.FillRectangle(expenseBrush, x + 2, bottom - b, width, b);
            TextRenderer.DrawText(g, p.Label, font, new Rectangle(x - step / 2, bottom + 9, step, 20), DesktopTheme.Muted, TextFormatFlags.HorizontalCenter);
        }
        TextRenderer.DrawText(g, "Receitas", font, new Point(left, 12), DesktopTheme.Accent);
        TextRenderer.DrawText(g, "Despesas", font, new Point(left + 100, 12), DesktopTheme.Negative);
    }
}
