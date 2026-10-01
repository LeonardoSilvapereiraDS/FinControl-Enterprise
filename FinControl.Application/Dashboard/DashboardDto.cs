namespace FinControl.Application.Dashboard;

public sealed record DashboardDto(
    string NomeUsuario,
    DateTime MesSelecionado,
    decimal SaldoGeral,
    decimal TotalReceitasMes,
    decimal TotalDespesasMes,
    decimal EconomiaMes,
    decimal? PercentualVariacaoReceitas,
    decimal? PercentualVariacaoDespesas,
    IReadOnlyList<ValorMensalDto> ReceitasPorMes,
    IReadOnlyList<ValorMensalDto> DespesasPorMes,
    IReadOnlyList<DespesaPorCategoriaDto> DespesasPorCategoria,
    decimal OrcamentoTotal,
    decimal OrcamentoUtilizado,
    decimal OrcamentoDisponivel,
    decimal PercentualOrcamentoUtilizado,
    IReadOnlyList<UltimaTransacaoDto> UltimasTransacoes)
{
    public bool PossuiTransacoes => UltimasTransacoes.Count > 0;
}
