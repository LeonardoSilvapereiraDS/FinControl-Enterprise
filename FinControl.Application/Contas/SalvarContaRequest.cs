using FinControl.Domain.Enums;

namespace FinControl.Application.Contas;

public sealed record SalvarContaRequest(
    string Nome,
    TipoConta TipoConta,
    decimal SaldoInicial);
