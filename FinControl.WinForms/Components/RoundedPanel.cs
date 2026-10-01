using System.Drawing.Drawing2D;
using FinControl.WinForms.Design;

namespace FinControl.WinForms.Components;

public class RoundedPanel : Panel
{
    public int CornerRadius { get; set; } = 16;

    public Color BorderColor { get; set; } = AppTheme.Border;

    public bool ShowBorder { get; set; } = true;

    public RoundedPanel()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.Panel;
        Padding = new Padding(16);
        Margin = new Padding(8);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        AplicarRegiao();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        if (!ShowBorder || Width <= 1 || Height <= 1)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = AppTheme.CreateRoundedRectangle(new Rectangle(0, 0, Width - 1, Height - 1), CornerRadius);
        using var pen = new Pen(BorderColor, 1F);

        e.Graphics.DrawPath(pen, path);
    }

    private void AplicarRegiao()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        using var path = AppTheme.CreateRoundedRectangle(new Rectangle(0, 0, Width, Height), CornerRadius);
        Region = new Region(path);
    }
}
