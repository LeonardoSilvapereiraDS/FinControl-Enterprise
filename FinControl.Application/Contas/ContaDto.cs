using FinControl.Domain.Enums;

namespace FinControl.Application.Contas;

public sealed record ContaDto(
    int Id,
    string Nome,
    TipoConta TipoConta,
    decimal SaldoInicial,
    bool Ativa);
