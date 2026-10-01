using FinControl.Domain.Enums;

namespace FinControl.Application.Dashboard;

public sealed record UltimaTransacaoDto(
    string Descricao,
    string Categoria,
    DateTime Data,
    string Conta,
    decimal Valor,
    TipoTransacao Tipo);
