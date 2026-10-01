using FinControl.WinForms.Design;

namespace FinControl.WinForms.Components;

public sealed class StyledDataGridView : DataGridView
{
    public StyledDataGridView()
    {
        Dock = DockStyle.Fill;
        AutoGenerateColumns = false;
        AllowUserToAddRows = false;
        AllowUserToDeleteRows = false;
        AllowUserToResizeRows = false;
        MultiSelect = false;
        ReadOnly = true;
        SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        RowHeadersVisible = false;

        ControlStyler.StyleGrid(this);
    }
}
