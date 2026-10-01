using FinControl.Application.Contas;
using FinControl.Domain.Enums;
using FinControl.WinForms.Design;

namespace FinControl.WinForms;

public sealed class ContaForm : Form
{
    private readonly TextBox _nomeTextBox = new();
    private readonly ComboBox _tipoComboBox = new();
    private readonly NumericUpDown _saldoInicialInput = new();

    public SalvarContaRequest Request { get; private set; } = new(string.Empty, TipoConta.ContaCorrente, 0m);

    public ContaForm(ContaDto? conta = null)
    {
        Text = conta is null ? "Nova conta" : "Editar conta";
        AutoScaleMode = AutoScaleMode.None;
        MinimumSize = new Size(600, 360);
        Size = new Size(640, 380);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
        Font = AppTheme.LabelFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        ConfigurarTela();

        _tipoComboBox.Items.AddRange(Enum.GetValues<TipoConta>().Cast<object>().ToArray());
        _tipoComboBox.SelectedItem = conta?.TipoConta ?? TipoConta.ContaCorrente;
        _nomeTextBox.Text = conta?.Nome ?? string.Empty;
        _saldoInicialInput.Value = conta?.SaldoInicial ?? 0m;
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

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        root.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = Text,
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        root.Controls.Add(CriarCampos(), 0, 1);
        root.Controls.Add(CriarAcoes(), 0, 2);

        Controls.Add(root);
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

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));

        ControlStyler.StyleInput(_nomeTextBox);
        ControlStyler.StyleInput(_tipoComboBox);
        ControlStyler.StyleInput(_saldoInicialInput);

        _tipoComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _saldoInicialInput.DecimalPlaces = 2;
        _saldoInicialInput.Maximum = 1_000_000_000;
        _saldoInicialInput.Minimum = -1_000_000_000;
        _saldoInicialInput.ThousandsSeparator = true;
        _saldoInicialInput.Increment = 100;

        var nomeCampo = CriarCampo("Nome", _nomeTextBox);
        grid.Controls.Add(nomeCampo, 0, 0);
        grid.SetColumnSpan(nomeCampo, 2);
        grid.Controls.Add(CriarCampo("Tipo", _tipoComboBox), 0, 1);
        grid.Controls.Add(CriarCampo("Saldo inicial", _saldoInicialInput), 1, 1);

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
        Request = new SalvarContaRequest(
            _nomeTextBox.Text,
            (TipoConta)_tipoComboBox.SelectedItem!,
            _saldoInicialInput.Value);

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
