using FinControl.Domain.Enums;

namespace FinControl.Application.Transacoes;

public sealed record TransacaoFiltro(
    DateTime? DataInicial = null,
    DateTime? DataFinal = null,
    TipoTransacao? Tipo = null,
    int? CategoriaId = null,
    int? ContaId = null);
