using FinControl.Domain.Enums;

namespace FinControl.Application.Transacoes;

public sealed record SalvarTransacaoRequest(
    string Descricao,
    decimal Valor,
    DateTime Data,
    TipoTransacao Tipo,
    int CategoriaId,
    int ContaId,
    string? Observacao,
    bool Pago,
    bool Recorrente);
