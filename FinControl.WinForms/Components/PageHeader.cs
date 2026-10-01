using FinControl.WinForms.Design;

namespace FinControl.WinForms.Components;

public sealed class PageHeader : UserControl
{
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly FlowLayoutPanel _actionsPanel;

    public PageHeader()
    {
        AutoScaleMode = AutoScaleMode.None;
        AutoSize = false;
        Height = 72;
        Dock = DockStyle.Fill;
        BackColor = Color.Transparent;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var textPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent
        };

        textPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        textPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));

        _titleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.TitleFont,
            ForeColor = AppTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _subtitleLabel = new Label
        {
            Dock = DockStyle.Fill,
            Font = AppTheme.SmallFont,
            ForeColor = AppTheme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        };

        textPanel.Controls.Add(_titleLabel, 0, 0);
        textPanel.Controls.Add(_subtitleLabel, 0, 1);

        _actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(12, 8, 0, 0)
        };

        root.Controls.Add(textPanel, 0, 0);
        root.Controls.Add(_actionsPanel, 1, 0);

        Controls.Add(root);
    }

    public void SetText(string title, string subtitle)
    {
        _titleLabel.Text = title;
        _subtitleLabel.Text = subtitle;
    }

    public void SetActions(params Control[] actions)
    {
        _actionsPanel.Controls.Clear();

        foreach (var action in actions)
        {
            action.Margin = new Padding(8, 0, 0, 0);
            _actionsPanel.Controls.Add(action);
        }
    }
}
