using System.Drawing.Drawing2D;

namespace FinControl.WinForms.Design;

internal static class AppTheme
{
    public static readonly Color Background = Color.FromArgb(17, 24, 39);
    public static readonly Color Shell = Color.FromArgb(15, 23, 42);
    public static readonly Color Sidebar = Color.FromArgb(15, 23, 42);
    public static readonly Color Panel = Color.FromArgb(24, 32, 51);
    public static readonly Color PanelAlt = Color.FromArgb(30, 41, 59);
    public static readonly Color Border = Color.FromArgb(51, 65, 85);
    public static readonly Color Text = Color.FromArgb(248, 250, 252);
    public static readonly Color MutedText = Color.FromArgb(148, 163, 184);
    public static readonly Color Purple = Color.FromArgb(139, 92, 246);
    public static readonly Color Magenta = Color.FromArgb(217, 70, 239);
    public static readonly Color Cyan = Color.FromArgb(56, 189, 248);
    public static readonly Color Blue = Color.FromArgb(56, 189, 248);
    public static readonly Color Green = Color.FromArgb(52, 211, 153);
    public static readonly Color Red = Color.FromArgb(251, 113, 133);
    public static readonly Color Warning = Color.FromArgb(251, 191, 36);

    public static readonly Font TitleFont = new("Segoe UI Semibold", 20F, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font SectionFont = new("Segoe UI Semibold", 12.5F, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font LabelFont = new("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font SmallFont = new("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font ValueFont = new("Segoe UI Semibold", 18F, FontStyle.Bold, GraphicsUnit.Point);

    public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }
}
