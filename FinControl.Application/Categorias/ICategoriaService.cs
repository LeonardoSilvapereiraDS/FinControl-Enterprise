namespace FinControl.Application.Categorias;

public interface ICategoriaService
{
    Task<IReadOnlyList<CategoriaDto>> ListarAsync(
        int usuarioId,
        bool incluirInativas = false,
        CancellationToken cancellationToken = default);

    Task<CategoriaDto> CriarAsync(
        int usuarioId,
        SalvarCategoriaRequest request,
        CancellationToken cancellationToken = default);

    Task<CategoriaDto> AtualizarAsync(
        int usuarioId,
        int categoriaId,
        SalvarCategoriaRequest request,
        CancellationToken cancellationToken = default);

    Task DesativarAsync(int usuarioId, int categoriaId, CancellationToken cancellationToken = default);

    Task ReativarAsync(int usuarioId, int categoriaId, CancellationToken cancellationToken = default);
}
