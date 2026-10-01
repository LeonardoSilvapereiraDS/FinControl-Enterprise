using FinControl.Application.Abstractions.Persistence;
using FinControl.Domain.Common;

namespace FinControl.Tests.Application;

internal sealed class RepositorioEmMemoria<TEntity> : IRepositorio<TEntity>
    where TEntity : Entidade
{
    private readonly List<TEntity> _entidades;

    public RepositorioEmMemoria(IEnumerable<TEntity>? entidades = null)
    {
        _entidades = entidades?.ToList() ?? [];
    }

    public Task<TEntity?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_entidades.SingleOrDefault(entidade => entidade.Id == id));
    }

    public Task<IReadOnlyList<TEntity>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TEntity>>(_entidades.ToList());
    }

    public Task AdicionarAsync(TEntity entidade, CancellationToken cancellationToken = default)
    {
        _entidades.Add(entidade);

        return Task.CompletedTask;
    }

    public void Atualizar(TEntity entidade)
    {
    }

    public void Remover(TEntity entidade)
    {
        _entidades.Remove(entidade);
    }
}
