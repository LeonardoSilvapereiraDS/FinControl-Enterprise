namespace FinControl.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardDto> ObterDashboardAsync(
        int usuarioId,
        DateTime mesSelecionado,
        CancellationToken cancellationToken = default);

    Task<DashboardResumoDto> ObterResumoAsync(
        int usuarioId,
        DateTime dataReferencia,
        CancellationToken cancellationToken = default);
}
