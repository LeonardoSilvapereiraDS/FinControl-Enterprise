namespace FinControl.Application.Dashboard;

public sealed record DespesaPorCategoriaDto(
    string Categoria,
    decimal Valor,
    decimal Percentual);
