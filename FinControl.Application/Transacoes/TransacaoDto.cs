using FinControl.Domain.Enums;

namespace FinControl.Application.Transacoes;

public sealed record TransacaoDto(
    int Id,
    string Descricao,
    decimal Valor,
    DateTime Data,
    TipoTransacao Tipo,
    int CategoriaId,
    string CategoriaNome,
    int ContaId,
    string ContaNome,
    string? Observacao,
    bool Pago,
    bool Recorrente);
