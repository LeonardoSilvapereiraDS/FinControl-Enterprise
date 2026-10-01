using FinControl.Application.Categorias;
using FinControl.Application.Contas;
using FinControl.Application.Transacoes;
using FinControl.Domain.Enums;
using FinControl.WinForms.Design;

namespace FinControl.WinForms;

public sealed class TransacaoForm : Form
{
    private readonly IReadOnlyList<CategoriaDto> _categorias;
    private readonly IReadOnlyList<ContaDto> _contas;
    private readonly TextBox _descricaoTextBox = new();
    private readonly ComboBox _tipoComboBox = new();
    private readonly NumericUpDown _valorInput = new();
    private readonly ComboBox _categoriaComboBox = new();
    private readonly ComboBox _contaComboBox = new();
    private readonly DateTimePicker _dataInput = new();
    private readonly CheckBox _pagoCheckBox = new();
    private readonly CheckBox _recorrenteCheckBox = new();
    private readonly TextBox _observacaoTextBox = new();
    private int? _categoriaSelecionadaInicial;

    public SalvarTransacaoRequest Request { get; private set; } = new(
        string.Empty,
        0m,
        DateTime.Today,
        TipoTransacao.Despesa,
        0,
        0,
        null,
        true,
        false);

    public TransacaoForm(
        IReadOnlyList<CategoriaDto> categorias,
        IReadOnlyList<ContaDto> contas,
        TransacaoDto? transacao = null)
    {
        _categorias = categorias;
        _contas = contas;
        _categoriaSelecionadaInicial = transacao?.CategoriaId;

        Text = transacao is null ? "Nova transacao" : "Editar transacao";
        AutoScaleMode = AutoScaleMode.None;
        MinimumSize = new Size(600, 600);
        Size = new Size(720, 640);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = AppTheme.Background;
        ForeColor = AppTheme.Text;
        Font = AppTheme.LabelFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        ConfigurarTela();
        CarregarCombos();
        Preencher(transacao);
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
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            BackColor = Color.Transparent
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));

        ControlStyler.StyleInput(_descricaoTextBox);
        ControlStyler.StyleInput(_tipoComboBox);
        ControlStyler.StyleInput(_valorInput);
        ControlStyler.StyleInput(_categoriaComboBox);
        ControlStyler.StyleInput(_contaComboBox);
        ControlStyler.StyleInput(_dataInput);
        ControlStyler.StyleInput(_observacaoTextBox);
        ControlStyler.StyleCheckBox(_pagoCheckBox);
        ControlStyler.StyleCheckBox(_recorrenteCheckBox);

        _tipoComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _tipoComboBox.SelectedIndexChanged += (_, _) => AtualizarCategorias();
        _categoriaComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _categoriaComboBox.DisplayMember = nameof(CategoriaDto.Nome);
        _contaComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _contaComboBox.DisplayMember = nameof(ContaDto.Nome);

        _valorInput.DecimalPlaces = 2;
        _valorInput.Maximum = 1_000_000_000;
        _valorInput.ThousandsSeparator = true;
        _valorInput.Increment = 10;
        _dataInput.Format = DateTimePickerFormat.Short;
        _observacaoTextBox.Multiline = true;
        _observacaoTextBox.ScrollBars = ScrollBars.Vertical;

        _pagoCheckBox.Text = "Pago";
        _recorrenteCheckBox.Text = "Recorrente";
        _pagoCheckBox.AutoSize = true;
        _recorrenteCheckBox.AutoSize = true;
        _pagoCheckBox.Margin = new Padding(0, 8, 24, 0);
        _recorrenteCheckBox.Margin = new Padding(0, 8, 0, 0);

        var descricaoCampo = CriarCampo("Descricao", _descricaoTextBox);
        grid.Controls.Add(descricaoCampo, 0, 0);
        grid.SetColumnSpan(descricaoCampo, 2);
        grid.Controls.Add(CriarCampo("Tipo", _tipoComboBox), 0, 1);
        grid.Controls.Add(CriarCampo("Valor", _valorInput), 1, 1);
        grid.Controls.Add(CriarCampo("Categoria", _categoriaComboBox), 0, 2);
        grid.Controls.Add(CriarCampo("Conta", _contaComboBox), 1, 2);
        var dataCampo = CriarCampo("Data", _dataInput);
        grid.Controls.Add(dataCampo, 0, 3);
        grid.SetColumnSpan(dataCampo, 2);

        var checks = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        checks.Controls.Add(_pagoCheckBox);
        checks.Controls.Add(_recorrenteCheckBox);
        grid.Controls.Add(checks, 0, 4);
        grid.SetColumnSpan(checks, 2);

        var observacaoCampo = CriarCampo("Observacao", _observacaoTextBox);
        grid.Controls.Add(observacaoCampo, 0, 5);
        grid.SetColumnSpan(observacaoCampo, 2);

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

    private void CarregarCombos()
    {
        _tipoComboBox.Items.AddRange(Enum.GetValues<TipoTransacao>().Cast<object>().ToArray());
        _tipoComboBox.SelectedItem = TipoTransacao.Despesa;
        _contaComboBox.DataSource = _contas.Where(conta => conta.Ativa).OrderBy(conta => conta.Nome).ToList();
        AtualizarCategorias();
    }

    private void Preencher(TransacaoDto? transacao)
    {
        if (transacao is null)
        {
            _dataInput.Value = DateTime.Today;
            _pagoCheckBox.Checked = true;
            return;
        }

        _descricaoTextBox.Text = transacao.Descricao;
        _tipoComboBox.SelectedItem = transacao.Tipo;
        _categoriaSelecionadaInicial = transacao.CategoriaId;
        AtualizarCategorias();
        SelecionarCategoria(transacao.CategoriaId);
        SelecionarConta(transacao.ContaId);
        _valorInput.Value = transacao.Valor;
        _dataInput.Value = transacao.Data;
        _pagoCheckBox.Checked = transacao.Pago;
        _recorrenteCheckBox.Checked = transacao.Recorrente;
        _observacaoTextBox.Text = transacao.Observacao;
    }

    private void AtualizarCategorias()
    {
        if (_tipoComboBox.SelectedItem is not TipoTransacao tipo)
        {
            return;
        }

        var tipoCategoria = tipo == TipoTransacao.Receita ? TipoCategoria.Receita : TipoCategoria.Despesa;
        _categoriaComboBox.DataSource = _categorias
            .Where(categoria => categoria.Ativa && categoria.Tipo == tipoCategoria)
            .OrderBy(categoria => categoria.Nome)
            .ToList();

        if (_categoriaSelecionadaInicial is int categoriaId)
        {
            SelecionarCategoria(categoriaId);
            _categoriaSelecionadaInicial = null;
        }
    }

    private void Salvar()
    {
        if (_categoriaComboBox.SelectedItem is not CategoriaDto categoria)
        {
            MessageBox.Show("Selecione uma categoria.", "FinControl", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_contaComboBox.SelectedItem is not ContaDto conta)
        {
            MessageBox.Show("Selecione uma conta.", "FinControl", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Request = new SalvarTransacaoRequest(
            _descricaoTextBox.Text,
            _valorInput.Value,
            _dataInput.Value.Date,
            (TipoTransacao)_tipoComboBox.SelectedItem!,
            categoria.Id,
            conta.Id,
            _observacaoTextBox.Text,
            _pagoCheckBox.Checked,
            _recorrenteCheckBox.Checked);

        DialogResult = DialogResult.OK;
    }

    private void SelecionarCategoria(int categoriaId)
    {
        foreach (var item in _categoriaComboBox.Items)
        {
            if (item is CategoriaDto categoria && categoria.Id == categoriaId)
            {
                _categoriaComboBox.SelectedItem = categoria;
                return;
            }
        }
    }

    private void SelecionarConta(int contaId)
    {
        foreach (var item in _contaComboBox.Items)
        {
            if (item is ContaDto conta && conta.Id == contaId)
            {
                _contaComboBox.SelectedItem = conta;
                return;
            }
        }
    }

    private static Control CriarCampo(string label, Control control)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0, 0, 16, 10),
            BackColor = Color.Transparent
        };

        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

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
