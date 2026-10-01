using FinControl.Application.Categorias;
using FinControl.Domain.Enums;
using FinControl.WinForms.Design;

namespace FinControl.WinForms;

public sealed class CategoriaForm : Form
{
    private readonly TextBox _nomeTextBox = new();
    private readonly ComboBox _tipoComboBox = new();

    public SalvarCategoriaRequest Request { get; private set; } = new(string.Empty, TipoCategoria.Despesa);

    public CategoriaForm(CategoriaDto? categoria = null)
    {
        Text = categoria is null ? "Nova categoria" : "Editar categoria";
        AutoScaleMode = AutoScaleMode.None;
        MinimumSize = new Size(600, 380);
        Size = new Size(620, 390);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
        Font = AppTheme.LabelFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        ConfigurarTela();

        _tipoComboBox.Items.AddRange(Enum.GetValues<TipoCategoria>().Cast<object>().ToArray());
        _tipoComboBox.SelectedItem = categoria?.Tipo ?? TipoCategoria.Despesa;
        _nomeTextBox.Text = categoria?.Nome ?? string.Empty;
    }

    private void ConfigurarTela()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(24),
            BackColor = AppTheme.Background
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        root.Controls.Add(CriarTitulo(), 0, 0);
        root.Controls.Add(CriarCampos(), 0, 1);
        root.Controls.Add(CriarAcoes(), 0, 2);

        Controls.Add(root);
    }

    private Control CriarTitulo()
    {
        return new Label
        {
            Dock = DockStyle.Fill,
            Text = Text,
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private Control CriarCampos()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            BackColor = Color.Transparent
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

        ControlStyler.StyleInput(_nomeTextBox);
        ControlStyler.StyleInput(_tipoComboBox);
        _tipoComboBox.DropDownStyle = ComboBoxStyle.DropDownList;

        var nomeCampo = CriarCampo("Nome", _nomeTextBox);
        grid.Controls.Add(nomeCampo, 0, 0);
        grid.SetColumnSpan(nomeCampo, 2);
        grid.Controls.Add(CriarCampo("Tipo", _tipoComboBox), 0, 1);

        return grid;
    }

    private Control CriarAcoes()
    {
        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var salvar = CriarBotao("Salvar", primary: true);
        var cancelar = CriarBotao("Cancelar", primary: false);

        salvar.Click += (_, _) => Salvar();
        cancelar.Click += (_, _) => DialogResult = DialogResult.Cancel;

        actions.Controls.Add(salvar);
        actions.Controls.Add(cancelar);

        return actions;
    }

    private void Salvar()
    {
        Request = new SalvarCategoriaRequest(_nomeTextBox.Text, (TipoCategoria)_tipoComboBox.SelectedItem!);
        DialogResult = DialogResult.OK;
    }

    private static Control CriarCampo(string label, Control control)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 16, 14),
            BackColor = Color.Transparent
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = label,
            ForeColor = AppTheme.MutedText,
            Font = AppTheme.SmallFont
        }, 0, 0);

        control.Dock = DockStyle.Fill;
        panel.Controls.Add(control, 0, 1);

        return panel;
    }

    private static Button CriarBotao(string text, bool primary)
    {
        var button = new Button
        {
            Width = 120,
            Height = 40,
            Text = text,
            Margin = new Padding(8, 6, 0, 0)
        };

        if (primary)
        {
            ControlStyler.StylePrimaryButton(button);
        }
        else
        {
            ControlStyler.StyleSecondaryButton(button);
        }

        return button;
    }
}
