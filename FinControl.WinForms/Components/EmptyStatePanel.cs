using FinControl.WinForms.Design;

namespace FinControl.WinForms.Components;

public sealed class EmptyStatePanel : RoundedPanel
{
    public EmptyStatePanel(string title, string description)
    {
        Dock = DockStyle.Fill;
        CornerRadius = 18;
        BackColor = AppTheme.Panel;
        Padding = new Padding(24);

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        stack.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = title,
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.Text,
            TextAlign = ContentAlignment.BottomCenter
        }, 0, 0);

        stack.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = description,
            Font = AppTheme.LabelFont,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.TopCenter
        }, 0, 1);

        Controls.Add(stack);
    }
}
