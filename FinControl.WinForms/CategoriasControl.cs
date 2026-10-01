using FinControl.Application.Categorias;
using FinControl.Domain.Enums;
using FinControl.Infrastructure.Persistence;
using FinControl.WinForms.Components;
using FinControl.WinForms.Design;

namespace FinControl.WinForms;

public sealed class CategoriasControl : UserControl
{
    private readonly ICategoriaService _categoriaService;
    private readonly BindingSource _bindingSource = new();
    private readonly StyledDataGridView _grid = new();
    private readonly TextBox _pesquisaTextBox = new();
    private readonly ComboBox _tipoFiltroComboBox = new();
    private readonly CheckBox _incluirInativasCheckBox = new();
    private readonly Label _statusLabel = new();
    private IReadOnlyList<CategoriaDto> _categorias = [];

    public event EventHandler? DadosAlterados;

    public CategoriasControl(ICategoriaService categoriaService)
    {
        _categoriaService = categoriaService;
        AutoScaleMode = AutoScaleMode.None;
        AutoSize = false;
        Margin = Padding.Empty;

        ConfigurarTela();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        try
        {
            _categorias = await _categoriaService.ListarAsync(
                BancoDadosInicializador.UsuarioPadraoId,
                _incluirInativasCheckBox.Checked);

            AplicarFiltros();
        }
        catch (Exception ex)
        {
            ExibirErro(ex);
        }
    }

    private void ConfigurarTela()
    {
        BackColor = AppTheme.Background;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = AppTheme.Background
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        root.Controls.Add(CriarCabecalho(), 0, 0);
        root.Controls.Add(CriarFiltros(), 0, 1);
        root.Controls.Add(CriarGridHost(), 0, 2);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.ForeColor = AppTheme.MutedText;
        _statusLabel.Font = AppTheme.SmallFont;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 3);

        Controls.Add(root);
    }

    private Control CriarCabecalho()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

        header.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Categorias",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var novo = CriarBotao("Novo", primary: true);
        novo.Click += async (_, _) => await AbrirFormularioAsync(null);
        header.Controls.Add(novo, 1, 0);

        return header;
    }

    private Control CriarFiltros()
    {
        var filtros = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 14,
            Padding = new Padding(16, 12, 16, 12),
            Margin = new Padding(0, 0, 0, 14),
            BackColor = AppTheme.Panel
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

        _pesquisaTextBox.PlaceholderText = "Pesquisar por nome";
        ControlStyler.StyleInput(_pesquisaTextBox);
        _pesquisaTextBox.TextChanged += (_, _) => AplicarFiltros();

        _tipoFiltroComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _tipoFiltroComboBox.Items.Add("Todos");
        _tipoFiltroComboBox.Items.Add(TipoCategoria.Receita);
        _tipoFiltroComboBox.Items.Add(TipoCategoria.Despesa);
        _tipoFiltroComboBox.SelectedIndex = 0;
        ControlStyler.StyleInput(_tipoFiltroComboBox);
        _tipoFiltroComboBox.SelectedIndexChanged += (_, _) => AplicarFiltros();

        _incluirInativasCheckBox.Text = "Inativas";
        _incluirInativasCheckBox.AutoSize = true;
        ControlStyler.StyleCheckBox(_incluirInativasCheckBox);
        _incluirInativasCheckBox.CheckedChanged += async (_, _) => await CarregarAsync();

        var atualizar = CriarBotao("Atualizar", primary: false);
        atualizar.Click += async (_, _) => await CarregarAsync();

        grid.Controls.Add(_pesquisaTextBox, 0, 0);
        grid.Controls.Add(_tipoFiltroComboBox, 1, 0);
        grid.Controls.Add(_incluirInativasCheckBox, 2, 0);
        grid.Controls.Add(atualizar, 3, 0);

        filtros.Controls.Add(grid);

        return filtros;
    }

    private Control CriarGridHost()
    {
        var host = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            CornerRadius = 18,
            Padding = new Padding(1),
            Margin = new Padding(0),
            BackColor = AppTheme.Panel
        };

        _grid.DataSource = _bindingSource;
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Nome",
            DataPropertyName = nameof(CategoriaDto.Nome),
            FillWeight = 52
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tipo",
            DataPropertyName = nameof(CategoriaDto.Tipo),
            FillWeight = 28
        });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Ativa",
            DataPropertyName = nameof(CategoriaDto.Ativa),
            FillWeight = 20
        });
        _grid.CellDoubleClick += async (_, _) => await EditarSelecionadaAsync();

        host.Controls.Add(_grid);

        return host;
    }

    private void AplicarFiltros()
    {
        var termo = _pesquisaTextBox.Text.Trim();
        var resultado = _categorias.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            resultado = resultado.Where(categoria =>
                categoria.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase));
        }

        if (_tipoFiltroComboBox.SelectedItem is TipoCategoria tipo)
        {
            resultado = resultado.Where(categoria => categoria.Tipo == tipo);
        }

        var lista = resultado.ToList();
        _bindingSource.DataSource = lista;
        _statusLabel.Text = lista.Count == 0 ? "Nenhuma categoria encontrada." : $"{lista.Count} categorias";
    }

    private async Task AbrirFormularioAsync(CategoriaDto? categoria)
    {
        using var form = new CategoriaForm(categoria);

        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            if (categoria is null)
            {
                await _categoriaService.CriarAsync(BancoDadosInicializador.UsuarioPadraoId, form.Request);
            }
            else
            {
                await _categoriaService.AtualizarAsync(BancoDadosInicializador.UsuarioPadraoId, categoria.Id, form.Request);
            }

            await CarregarAsync();
            DadosAlterados?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ExibirErro(ex);
        }
    }

    private async Task EditarSelecionadaAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is CategoriaDto categoria)
        {
            await AbrirFormularioAsync(categoria);
        }
    }

    private static Button CriarBotao(string texto, bool primary)
    {
        var button = new Button
        {
            Dock = DockStyle.Fill,
            Height = 40,
            Text = texto
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

    private static void ExibirErro(Exception ex)
    {
        MessageBox.Show(ex.Message, "FinControl", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
