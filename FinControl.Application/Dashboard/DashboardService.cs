using FinControl.Application.Abstractions.Persistence;
using FinControl.Domain.Entities;
using FinControl.Domain.Enums;

namespace FinControl.Application.Dashboard;

public sealed class DashboardService : IDashboardService
{
    private const int QuantidadeMesesGrafico = 6;
    private const int QuantidadeUltimasTransacoes = 6;

    private readonly IRepositorio<Categoria> _categorias;
    private readonly IRepositorio<Conta> _contas;
    private readonly IRepositorio<MetaFinanceira> _metas;
    private readonly IRepositorio<Orcamento> _orcamentos;
    private readonly IRepositorio<Transacao> _transacoes;
    private readonly IRepositorio<Usuario> _usuarios;

    public DashboardService(
        IRepositorio<Categoria> categorias,
        IRepositorio<Conta> contas,
        IRepositorio<MetaFinanceira> metas,
        IRepositorio<Orcamento> orcamentos,
        IRepositorio<Transacao> transacoes,
        IRepositorio<Usuario> usuarios)
    {
        _categorias = categorias;
        _contas = contas;
        _metas = metas;
        _orcamentos = orcamentos;
        _transacoes = transacoes;
        _usuarios = usuarios;
    }

    public async Task<DashboardDto> ObterDashboardAsync(
        int usuarioId,
        DateTime mesSelecionado,
        CancellationToken cancellationToken = default)
    {
        var usuarios = await _usuarios.ListarAsync(cancellationToken);
        var transacoes = await _transacoes.ListarAsync(cancellationToken);
        var contas = await _contas.ListarAsync(cancellationToken);
        var categorias = await _categorias.ListarAsync(cancellationToken);
        var orcamentos = await _orcamentos.ListarAsync(cancellationToken);

        var usuario = usuarios.FirstOrDefault(item => item.Id == usuarioId);
        var inicioMes = new DateTime(mesSelecionado.Year, mesSelecionado.Month, 1);
        var fimMes = inicioMes.AddMonths(1).AddDays(-1);
        var inicioMesAnterior = inicioMes.AddMonths(-1);
        var fimMesAnterior = inicioMes.AddDays(-1);

        var transacoesDoUsuario = transacoes
            .Where(transacao => transacao.UsuarioId == usuarioId && transacao.Pago)
            .ToList();

        var transacoesDoMes = transacoesDoUsuario
            .Where(transacao => transacao.Data.Date >= inicioMes && transacao.Data.Date <= fimMes)
            .ToList();

        var transacoesDoMesAnterior = transacoesDoUsuario
            .Where(transacao => transacao.Data.Date >= inicioMesAnterior && transacao.Data.Date <= fimMesAnterior)
            .ToList();

        var receitasMes = SomarPorTipo(transacoesDoMes, TipoTransacao.Receita);
        var despesasMes = SomarPorTipo(transacoesDoMes, TipoTransacao.Despesa);
        var receitasMesAnterior = SomarPorTipo(transacoesDoMesAnterior, TipoTransacao.Receita);
        var despesasMesAnterior = SomarPorTipo(transacoesDoMesAnterior, TipoTransacao.Despesa);

        var saldoInicial = contas
            .Where(conta => conta.UsuarioId == usuarioId)
            .Sum(conta => conta.SaldoInicial);

        var saldoGeral = saldoInicial + transacoesDoUsuario.Sum(transacao => transacao.ValorComSinal);
        var economiaMes = receitasMes - despesasMes;
        var categoriasPorId = categorias.ToDictionary(categoria => categoria.Id);
        var contasPorId = contas.ToDictionary(conta => conta.Id);

        var mesesGrafico = Enumerable
            .Range(0, QuantidadeMesesGrafico)
            .Select(offset => inicioMes.AddMonths(offset - QuantidadeMesesGrafico + 1))
            .ToList();

        var receitasPorMes = CriarSerieMensal(transacoesDoUsuario, mesesGrafico, TipoTransacao.Receita);
        var despesasPorMes = CriarSerieMensal(transacoesDoUsuario, mesesGrafico, TipoTransacao.Despesa);
        var despesasPorCategoria = CriarDespesasPorCategoria(transacoesDoMes, categoriasPorId);
        var dadosOrcamento = CriarDadosOrcamento(usuarioId, inicioMes, transacoesDoMes, orcamentos);
        var ultimasTransacoes = CriarUltimasTransacoes(transacoesDoMes, categoriasPorId, contasPorId);

        return new DashboardDto(
            usuario?.Nome ?? "Usuario Local",
            inicioMes,
            saldoGeral,
            receitasMes,
            despesasMes,
            economiaMes,
            CalcularVariacao(receitasMes, receitasMesAnterior),
            CalcularVariacao(despesasMes, despesasMesAnterior),
            receitasPorMes,
            despesasPorMes,
            despesasPorCategoria,
            dadosOrcamento.Total,
            dadosOrcamento.Utilizado,
            dadosOrcamento.Disponivel,
            dadosOrcamento.PercentualUtilizado,
            ultimasTransacoes);
    }

    public async Task<DashboardResumoDto> ObterResumoAsync(
        int usuarioId,
        DateTime dataReferencia,
        CancellationToken cancellationToken = default)
    {
        var transacoes = await _transacoes.ListarAsync(cancellationToken);
        var contas = await _contas.ListarAsync(cancellationToken);
        var metas = await _metas.ListarAsync(cancellationToken);

        var transacoesDoUsuario = transacoes
            .Where(transacao => transacao.UsuarioId == usuarioId && transacao.Pago)
            .ToList();

        var transacoesDoMes = transacoesDoUsuario
            .Where(transacao =>
                transacao.Data.Month == dataReferencia.Month &&
                transacao.Data.Year == dataReferencia.Year)
            .ToList();

        var receitasMes = transacoesDoMes
            .Where(transacao => transacao.Tipo == TipoTransacao.Receita)
            .Sum(transacao => transacao.Valor);

        var despesasMes = transacoesDoMes
            .Where(transacao => transacao.Tipo == TipoTransacao.Despesa)
            .Sum(transacao => transacao.Valor);

        var saldoInicial = contas
            .Where(conta => conta.UsuarioId == usuarioId)
            .Sum(conta => conta.SaldoInicial);

        var saldoAtual = saldoInicial + transacoesDoUsuario.Sum(transacao => transacao.ValorComSinal);

        var contasAtivas = contas.Count(conta => conta.UsuarioId == usuarioId && conta.Ativa);
        var metasEmAndamento = metas.Count(meta => meta.UsuarioId == usuarioId && !meta.Concluida);

        return new DashboardResumoDto(
            receitasMes,
            despesasMes,
            saldoAtual,
            transacoesDoMes.Count,
            contasAtivas,
            metasEmAndamento);
    }

    private static decimal SomarPorTipo(IEnumerable<Transacao> transacoes, TipoTransacao tipo)
    {
        return transacoes
            .Where(transacao => transacao.Tipo == tipo)
            .Sum(transacao => transacao.Valor);
    }

    private static IReadOnlyList<ValorMensalDto> CriarSerieMensal(
        IReadOnlyList<Transacao> transacoes,
        IReadOnlyList<DateTime> meses,
        TipoTransacao tipo)
    {
        return meses
            .Select(mes => new ValorMensalDto(
                mes,
                transacoes
                    .Where(transacao =>
                        transacao.Tipo == tipo &&
                        transacao.Data.Month == mes.Month &&
                        transacao.Data.Year == mes.Year)
                    .Sum(transacao => transacao.Valor)))
            .ToList();
    }

    private static IReadOnlyList<DespesaPorCategoriaDto> CriarDespesasPorCategoria(
        IReadOnlyList<Transacao> transacoesDoMes,
        IReadOnlyDictionary<int, Categoria> categoriasPorId)
    {
        var despesas = transacoesDoMes
            .Where(transacao => transacao.Tipo == TipoTransacao.Despesa)
            .ToList();

        var total = despesas.Sum(transacao => transacao.Valor);

        if (total <= 0)
        {
            return [];
        }

        return despesas
            .GroupBy(transacao => transacao.CategoriaId)
            .Select(grupo =>
            {
                var valor = grupo.Sum(transacao => transacao.Valor);
                var categoria = categoriasPorId.GetValueOrDefault(grupo.Key)?.Nome ?? "Sem categoria";

                return new DespesaPorCategoriaDto(
                    categoria,
                    valor,
                    Math.Round(valor / total * 100m, 2));
            })
            .OrderByDescending(item => item.Valor)
            .ToList();
    }

    private static DadosOrcamento CriarDadosOrcamento(
        int usuarioId,
        DateTime inicioMes,
        IReadOnlyList<Transacao> transacoesDoMes,
        IReadOnlyList<Orcamento> orcamentos)
    {
        var orcamentosDoMes = orcamentos
            .Where(orcamento =>
                orcamento.UsuarioId == usuarioId &&
                orcamento.Mes == inicioMes.Month &&
                orcamento.Ano == inicioMes.Year)
            .ToList();

        var total = orcamentosDoMes.Sum(orcamento => orcamento.ValorLimite);

        if (total <= 0)
        {
            return new DadosOrcamento(0m, 0m, 0m, 0m);
        }

        var categoriasComOrcamento = orcamentosDoMes
            .Select(orcamento => orcamento.CategoriaId)
            .ToHashSet();

        var utilizado = transacoesDoMes
            .Where(transacao =>
                transacao.Tipo == TipoTransacao.Despesa &&
                categoriasComOrcamento.Contains(transacao.CategoriaId))
            .Sum(transacao => transacao.Valor);

        var disponivel = total - utilizado;
        var percentual = Math.Round(utilizado / total * 100m, 2);

        return new DadosOrcamento(total, utilizado, disponivel, percentual);
    }

    private static IReadOnlyList<UltimaTransacaoDto> CriarUltimasTransacoes(
        IReadOnlyList<Transacao> transacoes,
        IReadOnlyDictionary<int, Categoria> categoriasPorId,
        IReadOnlyDictionary<int, Conta> contasPorId)
    {
        return transacoes
            .OrderByDescending(transacao => transacao.Data)
            .ThenByDescending(transacao => transacao.Id)
            .Take(QuantidadeUltimasTransacoes)
            .Select(transacao => new UltimaTransacaoDto(
                transacao.Descricao,
                categoriasPorId.GetValueOrDefault(transacao.CategoriaId)?.Nome ?? "Sem categoria",
                transacao.Data,
                contasPorId.GetValueOrDefault(transacao.ContaId)?.Nome ?? "Sem conta",
                transacao.Valor,
                transacao.Tipo))
            .ToList();
    }

    private static decimal? CalcularVariacao(decimal valorAtual, decimal valorAnterior)
    {
        if (valorAnterior == 0m)
        {
            return null;
        }

        return Math.Round((valorAtual - valorAnterior) / valorAnterior * 100m, 2);
    }

    private sealed record DadosOrcamento(
        decimal Total,
        decimal Utilizado,
        decimal Disponivel,
        decimal PercentualUtilizado);
}
