using System.Globalization;
using FinControl.Application.Dashboard;
using FinControl.Domain.Enums;
using FinControl.WinForms.Design;

namespace FinControl.WinForms.Components;

public sealed class TransactionItem : RoundedPanel
{
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("pt-BR");

    public TransactionItem(UltimaTransacaoDto transacao)
    {
        CornerRadius = 12;
        BackColor = AppTheme.PanelAlt;
        Height = 76;
        Dock = DockStyle.Top;
        Margin = new Padding(0, 0, 0, 8);
        Padding = new Padding(12);

        var valorCor = transacao.Tipo == TipoTransacao.Receita ? AppTheme.Green : AppTheme.Red;
        var sinal = transacao.Tipo == TipoTransacao.Receita ? "+" : "-";

        Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Left,
            Width = 44,
            Text = transacao.Tipo == TipoTransacao.Receita ? "+" : "-",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = valorCor
        });

        Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Right,
            Width = 118,
            Text = $"{sinal}{transacao.Valor.ToString("C", _culture)}",
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = valorCor
        });

        var info = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        info.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        info.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = transacao.Descricao,
            Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = AppTheme.Text
        }, 0, 0);

        info.Controls.Add(new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Text = $"{transacao.Categoria} | {transacao.Conta} | {transacao.Data:dd/MM/yyyy}",
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.MutedText
        }, 0, 1);

        Controls.Add(info);
    }
}
