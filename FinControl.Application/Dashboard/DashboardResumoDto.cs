namespace FinControl.Application.Dashboard;

public sealed record DashboardResumoDto(
    decimal ReceitasMes,
    decimal DespesasMes,
    decimal SaldoAtual,
    int TotalTransacoesMes,
    int ContasAtivas,
    int MetasEmAndamento);
