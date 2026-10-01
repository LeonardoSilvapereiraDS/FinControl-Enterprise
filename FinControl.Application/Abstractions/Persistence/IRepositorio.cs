using FinControl.Domain.Common;

namespace FinControl.Application.Abstractions.Persistence;

public interface IRepositorio<TEntity>
    where TEntity : Entidade
{
    Task<TEntity?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> ListarAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(TEntity entidade, CancellationToken cancellationToken = default);

    void Atualizar(TEntity entidade);

    void Remover(TEntity entidade);
}
