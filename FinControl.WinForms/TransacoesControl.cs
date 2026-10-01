using FinControl.Application.Categorias;
using FinControl.Application.Contas;
using FinControl.Application.Transacoes;
using FinControl.Domain.Enums;
using FinControl.Infrastructure.Persistence;
using FinControl.WinForms.Components;
using FinControl.WinForms.Design;

namespace FinControl.WinForms;

public sealed class TransacoesControl : UserControl
{
    private readonly ICategoriaService _categoriaService;
    private readonly IContaService _contaService;
    private readonly ITransacaoService _transacaoService;
    private readonly BindingSource _bindingSource = new();
    private readonly StyledDataGridView _grid = new();
    private readonly TextBox _pesquisaTextBox = new();
    private readonly ComboBox _tipoFiltroComboBox = new();
    private readonly Label _statusLabel = new();
    private IReadOnlyList<CategoriaDto> _categorias = [];
    private IReadOnlyList<ContaDto> _contas = [];
    private IReadOnlyList<TransacaoDto> _transacoes = [];

    public event EventHandler? DadosAlterados;

    public TransacoesControl(
        ICategoriaService categoriaService,
        IContaService contaService,
        ITransacaoService transacaoService)
    {
        _categoriaService = categoriaService;
        _contaService = contaService;
        _transacaoService = transacaoService;
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

    public async Task AbrirNovaTransacaoAsync()
    {
        try { await GarantirDadosAuxiliaresAsync(); await AbrirFormularioAsync(null); }
        catch (Exception ex) { ExibirErro(ex); }
    }

    public void DefinirFiltroTipo(TipoTransacao? tipo)
    {
        _tipoFiltroComboBox.SelectedIndex = tipo == TipoTransacao.Receita ? 1 : tipo == TipoTransacao.Despesa ? 2 : 0;
        AplicarFiltros();
    }

    private async Task CarregarAsync()
    {
        try
        {
            await GarantirDadosAuxiliaresAsync();
            _transacoes = await _transacaoService.ListarAsync(BancoDadosInicializador.UsuarioPadraoId);
            AplicarFiltros();
        }
        catch (Exception ex)
        {
            ExibirErro(ex);
        }
    }

    private async Task GarantirDadosAuxiliaresAsync()
    {
        _categorias = await _categoriaService.ListarAsync(BancoDadosInicializador.UsuarioPadraoId);
        _contas = await _contaService.ListarAsync(BancoDadosInicializador.UsuarioPadraoId);
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
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

        header.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Transacoes",
            Font = AppTheme.SectionFont,
            ForeColor = AppTheme.Text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var novo = CriarBotao("Nova transacao", primary: true);
        novo.Click += async (_, _) => await AbrirNovaTransacaoAsync();

        var remover = CriarBotao("Remover", primary: false);
        remover.Click += async (_, _) => await RemoverSelecionadaAsync();

        header.Controls.Add(novo, 1, 0);
        header.Controls.Add(remover, 2, 0);

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
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };

        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));

        _pesquisaTextBox.PlaceholderText = "Pesquisar por descricao, categoria ou conta";
        ControlStyler.StyleInput(_pesquisaTextBox);
        _pesquisaTextBox.TextChanged += (_, _) => AplicarFiltros();

        _tipoFiltroComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _tipoFiltroComboBox.Items.Add("Todos");
        _tipoFiltroComboBox.Items.Add(TipoTransacao.Receita);
        _tipoFiltroComboBox.Items.Add(TipoTransacao.Despesa);
        _tipoFiltroComboBox.SelectedIndex = 0;
        ControlStyler.StyleInput(_tipoFiltroComboBox);
        _tipoFiltroComboBox.SelectedIndexChanged += (_, _) => AplicarFiltros();

        var atualizar = CriarBotao("Atualizar", primary: false);
        atualizar.Click += async (_, _) => await CarregarAsync();

        grid.Controls.Add(_pesquisaTextBox, 0, 0);
        grid.Controls.Add(_tipoFiltroComboBox, 1, 0);
        grid.Controls.Add(atualizar, 2, 0);

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
            HeaderText = "Data",
            DataPropertyName = nameof(TransacaoDto.Data),
            DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" },
            FillWeight = 14
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Descricao",
            DataPropertyName = nameof(TransacaoDto.Descricao),
            FillWeight = 28
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Tipo",
            DataPropertyName = nameof(TransacaoDto.Tipo),
            FillWeight = 14
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Categoria",
            DataPropertyName = nameof(TransacaoDto.CategoriaNome),
            FillWeight = 20
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Conta",
            DataPropertyName = nameof(TransacaoDto.ContaNome),
            FillWeight = 18
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Valor",
            DataPropertyName = nameof(TransacaoDto.Valor),
            DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" },
            FillWeight = 16
        });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Pago",
            DataPropertyName = nameof(TransacaoDto.Pago),
            FillWeight = 10
        });

        _grid.CellDoubleClick += async (_, _) => await EditarSelecionadaAsync();
        host.Controls.Add(_grid);

        return host;
    }

    private void AplicarFiltros()
    {
        var termo = _pesquisaTextBox.Text.Trim();
        var resultado = _transacoes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(termo))
        {
            resultado = resultado.Where(transacao =>
                transacao.Descricao.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                transacao.CategoriaNome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
                transacao.ContaNome.Contains(termo, StringComparison.OrdinalIgnoreCase));
        }

        if (_tipoFiltroComboBox.SelectedItem is TipoTransacao tipo)
        {
            resultado = resultado.Where(transacao => transacao.Tipo == tipo);
        }

        var lista = resultado.ToList();
        _bindingSource.DataSource = lista;
        _statusLabel.Text = lista.Count == 0 ? "Nenhuma transacao encontrada." : $"{lista.Count} transacoes";
    }

    private async Task AbrirFormularioAsync(TransacaoDto? transacao)
    {
        using var form = new TransacaoForm(_categorias, _contas, transacao);

        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            if (transacao is null)
            {
                await _transacaoService.CriarAsync(BancoDadosInicializador.UsuarioPadraoId, form.Request);
            }
            else
            {
                await _transacaoService.AtualizarAsync(BancoDadosInicializador.UsuarioPadraoId, transacao.Id, form.Request);
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
        if (_grid.CurrentRow?.DataBoundItem is TransacaoDto transacao)
        {
            await GarantirDadosAuxiliaresAsync();
            await AbrirFormularioAsync(transacao);
        }
    }

    private async Task RemoverSelecionadaAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not TransacaoDto transacao)
        {
            return;
        }

        var result = MessageBox.Show(
            "Remover transacao selecionada?",
            "FinControl",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _transacaoService.RemoverAsync(BancoDadosInicializador.UsuarioPadraoId, transacao.Id);
            await CarregarAsync();
            DadosAlterados?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            ExibirErro(ex);
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
