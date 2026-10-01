namespace FinControl.Application.Transacoes;

public interface ITransacaoService
{
    Task<IReadOnlyList<TransacaoDto>> ListarAsync(
        int usuarioId,
        TransacaoFiltro? filtro = null,
        CancellationToken cancellationToken = default);

    Task<TransacaoDto> CriarAsync(
        int usuarioId,
        SalvarTransacaoRequest request,
        CancellationToken cancellationToken = default);

    Task<TransacaoDto> AtualizarAsync(
        int usuarioId,
        int transacaoId,
        SalvarTransacaoRequest request,
        CancellationToken cancellationToken = default);

    Task RemoverAsync(int usuarioId, int transacaoId, CancellationToken cancellationToken = default);
}
