namespace FinControl.Application.Contas;

public interface IContaService
{
    Task<IReadOnlyList<ContaDto>> ListarAsync(
        int usuarioId,
        bool incluirInativas = false,
        CancellationToken cancellationToken = default);

    Task<ContaDto> CriarAsync(
        int usuarioId,
        SalvarContaRequest request,
        CancellationToken cancellationToken = default);

    Task<ContaDto> AtualizarAsync(
        int usuarioId,
        int contaId,
        SalvarContaRequest request,
        CancellationToken cancellationToken = default);

    Task DesativarAsync(int usuarioId, int contaId, CancellationToken cancellationToken = default);

    Task ReativarAsync(int usuarioId, int contaId, CancellationToken cancellationToken = default);
}
