using FinControl.WinForms.Design;

namespace FinControl.WinForms.Components;

public sealed class ChartPanel : RoundedPanel
{
    private readonly Label _titleLabel;
    private readonly Panel _contentHost;

    public ChartPanel(string titulo)
    {
        CornerRadius = 14;
        Dock = DockStyle.Fill;
        BackColor = AppTheme.Panel;
        Padding = new Padding(12);
        Margin = new Padding(5);

        _titleLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 30,
            Text = titulo,
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.Text
        };

        _contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };

        Controls.Add(_contentHost);
        Controls.Add(_titleLabel);
    }

    public void SetContent(Control control)
    {
        _contentHost.Controls.Clear();
        control.Dock = DockStyle.Fill;
        _contentHost.Controls.Add(control);
    }
}
