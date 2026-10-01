using FinControl.WinForms.Design;

namespace FinControl.WinForms.Components;

public sealed class SummaryCard : RoundedPanel
{
    private readonly Label _titleLabel;
    private readonly Label _valueLabel;
    private readonly Label _variationLabel;

    public SummaryCard()
    {
        CornerRadius = 14;
        Dock = DockStyle.Fill;
        BackColor = AppTheme.Panel;
        Padding = new Padding(12);
        Margin = new Padding(5);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        _titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.LabelFont,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        _valueLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.ValueFont,
            ForeColor = AppTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        _variationLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true
        };

        root.Controls.Add(_titleLabel, 0, 0);
        root.Controls.Add(_valueLabel, 0, 1);
        root.Controls.Add(_variationLabel, 0, 2);

        Controls.Add(root);
    }

    public void Atualizar(string titulo, string valor, string icone, Color cor, string variacao)
    {
        _titleLabel.Text = string.IsNullOrWhiteSpace(icone) ? titulo : $"{icone}  {titulo}";
        _valueLabel.Text = valor;
        _valueLabel.ForeColor = cor;
        _variationLabel.Text = variacao;
        _variationLabel.ForeColor = variacao.StartsWith("-", StringComparison.Ordinal)
            ? AppTheme.Red
            : variacao.StartsWith("Sem", StringComparison.OrdinalIgnoreCase)
                ? AppTheme.MutedText
                : AppTheme.Green;
    }
}
