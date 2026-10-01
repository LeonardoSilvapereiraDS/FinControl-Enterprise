using System.Drawing.Drawing2D;
using FinControl.WinForms.Design;

namespace FinControl.WinForms.Components;

public sealed class MenuButton : Button
{
    private bool _selected;

    public string MenuKey { get; }

    public Color AccentColor { get; set; } = AppTheme.Purple;

    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            Invalidate();
        }
    }

    public MenuButton(string menuKey, string text)
    {
        MenuKey = menuKey;
        Text = text;
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold, GraphicsUnit.Point);
        Height = 42;
        Margin = new Padding(0, 2, 0, 2);
        Padding = new Padding(36, 0, 8, 0);
        TextAlign = ContentAlignment.MiddleLeft;
        ForeColor = AppTheme.MutedText;
        BackColor = Color.Transparent;
        UseVisualStyleBackColor = false;
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        pevent.Graphics.Clear(AppTheme.Sidebar);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = AppTheme.CreateRoundedRectangle(bounds, 12);

        if (Selected)
        {
            using var brush = new SolidBrush(AppTheme.PanelAlt);
            pevent.Graphics.FillPath(brush, path);

            using var accent = new SolidBrush(AccentColor);
            pevent.Graphics.FillRectangle(accent, new Rectangle(0, 9, 4, Height - 18));
        }
        else if (ClientRectangle.Contains(PointToClient(Cursor.Position)))
        {
            using var brush = new SolidBrush(Color.FromArgb(25, 35, 55));
            pevent.Graphics.FillPath(brush, path);
        }

        using var iconPen = new Pen(Selected ? AccentColor : AppTheme.Border, 2F);
        pevent.Graphics.DrawEllipse(iconPen, 15, Height / 2 - 5, 10, 10);

        var textRect = new Rectangle(32, 0, Width - 38, Height);
        using var textFont = ObterFonteParaTexto(pevent.Graphics, textRect.Width);

        TextRenderer.DrawText(
            pevent.Graphics,
            Text,
            textFont,
            textRect,
            Selected ? AppTheme.Text : ForeColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        Invalidate();
    }

    private Font ObterFonteParaTexto(Graphics graphics, int larguraDisponivel)
    {
        for (var size = Font.Size; size >= 8.5F; size -= 0.25F)
        {
            var candidate = new Font(Font.FontFamily, size, Font.Style, GraphicsUnit.Point);
            var measured = TextRenderer.MeasureText(graphics, Text, candidate, new Size(int.MaxValue, Height), TextFormatFlags.NoPadding);

            if (measured.Width <= larguraDisponivel)
            {
                return candidate;
            }

            candidate.Dispose();
        }

        return new Font(Font.FontFamily, 8.5F, Font.Style, GraphicsUnit.Point);
    }
}
