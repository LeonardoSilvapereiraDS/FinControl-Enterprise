using System.Drawing.Drawing2D;
using System.Globalization;
using FinControl.WinForms.Design;

namespace FinControl.WinForms.Components;

public sealed class BudgetProgressCard : RoundedPanel
{
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("pt-BR");

    public decimal Total { get; private set; }

    public decimal Utilizado { get; private set; }

    public decimal Disponivel { get; private set; }

    public decimal Percentual { get; private set; }

    public BudgetProgressCard()
    {
        CornerRadius = 14;
        Dock = DockStyle.Fill;
        BackColor = AppTheme.Panel;
        Margin = new Padding(5);
        MinimumSize = new Size(190, 180);
    }

    public void Atualizar(decimal total, decimal utilizado, decimal disponivel, decimal percentual)
    {
        Total = total;
        Utilizado = utilizado;
        Disponivel = disponivel;
        Percentual = percentual;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var titleBrush = new SolidBrush(AppTheme.Text);
        using var mutedBrush = new SolidBrush(AppTheme.MutedText);
        using var accentBrush = new SolidBrush(ObterCorPercentual());
        using var trackPen = new Pen(AppTheme.PanelAlt, 14) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        using var valuePen = new Pen(ObterCorPercentual(), 14) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        e.Graphics.DrawString("Orcamento mensal", AppTheme.SectionFont, titleBrush, 16, 14);

        var compact = Height < 230 || Width < 360;
        var progressSize = compact
            ? Math.Max(58, Math.Min(78, Math.Min(Width - 210, Height - 98)))
            : Math.Max(82, Math.Min(112, Math.Min(Width - 92, Height - 120)));

        var rect = compact
            ? new Rectangle(22, 60, progressSize, progressSize)
            : new Rectangle(42, 54, progressSize, progressSize);

        e.Graphics.DrawArc(trackPen, rect, -90, 360);
        e.Graphics.DrawArc(valuePen, rect, -90, (float)Math.Min(360m, Percentual / 100m * 360m));

        var percentText = Total <= 0 ? "--" : $"{Percentual:N0}%";
        using var percentFont = CriarFonteAjustada(e.Graphics, percentText, rect.Width - 8, compact ? 15F : 18F, 10F);
        var percentSize = e.Graphics.MeasureString(percentText, percentFont);
        e.Graphics.DrawString(
            percentText,
            percentFont,
            accentBrush,
            rect.Left + (rect.Width - percentSize.Width) / 2,
            rect.Top + (rect.Height - percentSize.Height) / 2);

        if (compact)
        {
            var detailsX = rect.Right + 18;
            var detailsY = rect.Top + 2;
            e.Graphics.DrawString($"Total {Total.ToString("C", _culture)}", AppTheme.SmallFont, mutedBrush, detailsX, detailsY);
            e.Graphics.DrawString($"Usado {Utilizado.ToString("C", _culture)}", AppTheme.SmallFont, mutedBrush, detailsX, detailsY + 22);
            e.Graphics.DrawString($"Livre {Disponivel.ToString("C", _culture)}", AppTheme.SmallFont, mutedBrush, detailsX, detailsY + 44);
            return;
        }

        var y = rect.Bottom + 14;
        e.Graphics.DrawString($"Total {Total.ToString("C", _culture)}", AppTheme.LabelFont, mutedBrush, 16, y);
        e.Graphics.DrawString($"Usado {Utilizado.ToString("C", _culture)}", AppTheme.LabelFont, mutedBrush, 16, y + 22);
        e.Graphics.DrawString($"Livre {Disponivel.ToString("C", _culture)}", AppTheme.LabelFont, mutedBrush, 16, y + 44);
    }

    private Color ObterCorPercentual()
    {
        if (Total <= 0)
        {
            return AppTheme.MutedText;
        }

        if (Percentual > 100m)
        {
            return AppTheme.Red;
        }

        if (Percentual > 90m)
        {
            return AppTheme.Red;
        }

        if (Percentual >= 70m)
        {
            return AppTheme.Warning;
        }

        return AppTheme.Green;
    }

    private static Font CriarFonteAjustada(Graphics graphics, string text, int larguraMaxima, float tamanhoInicial, float tamanhoMinimo)
    {
        for (var size = tamanhoInicial; size >= tamanhoMinimo; size -= 0.5F)
        {
            var font = new Font("Segoe UI Semibold", size, FontStyle.Bold, GraphicsUnit.Point);
            var measured = graphics.MeasureString(text, font);

            if (measured.Width <= larguraMaxima)
            {
                return font;
            }

            font.Dispose();
        }

        return new Font("Segoe UI Semibold", tamanhoMinimo, FontStyle.Bold, GraphicsUnit.Point);
    }
}
