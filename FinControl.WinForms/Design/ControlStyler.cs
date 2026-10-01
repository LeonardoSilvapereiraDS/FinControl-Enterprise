namespace FinControl.WinForms.Design;

internal static class ControlStyler
{
    public static void StyleGrid(DataGridView grid)
    {
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = AppTheme.Panel;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.GridColor = AppTheme.Border;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 42;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.ColumnHeadersDefaultCellStyle.BackColor = AppTheme.PanelAlt;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.Text;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = AppTheme.PanelAlt;
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = AppTheme.Text;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point);
        grid.DefaultCellStyle.BackColor = AppTheme.Panel;
        grid.DefaultCellStyle.ForeColor = AppTheme.Text;
        grid.DefaultCellStyle.SelectionBackColor = AppTheme.Purple;
        grid.DefaultCellStyle.SelectionForeColor = AppTheme.Text;
        grid.DefaultCellStyle.Font = AppTheme.LabelFont;
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(31, 38, 62);
        grid.RowTemplate.Height = 38;
    }

    public static void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = AppTheme.Purple;
        button.ForeColor = AppTheme.Text;
        button.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold, GraphicsUnit.Point);
        button.MinimumSize = new Size(0, 38);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    public static void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = AppTheme.Border;
        button.BackColor = AppTheme.PanelAlt;
        button.ForeColor = AppTheme.Text;
        button.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);
        button.MinimumSize = new Size(0, 38);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
    }

    public static void StyleInput(Control control)
    {
        control.BackColor = AppTheme.PanelAlt;
        control.ForeColor = AppTheme.Text;
        control.Font = AppTheme.LabelFont;
        control.MinimumSize = new Size(0, 36);
    }

    public static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.BackColor = Color.Transparent;
        checkBox.ForeColor = AppTheme.MutedText;
        checkBox.Font = AppTheme.LabelFont;
    }
}
