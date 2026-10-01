using FinControl.Application.Abstractions.Persistence;
using FinControl.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FinControl.Infrastructure.Persistence.Repositories;

public sealed class Repositorio<TEntity> : IRepositorio<TEntity>
    where TEntity : Entidade
{
    private readonly FinControlDbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public Repositorio(FinControlDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public async Task<TEntity?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AdicionarAsync(TEntity entidade, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entidade, cancellationToken);
    }

    public void Atualizar(TEntity entidade)
    {
        _dbSet.Update(entidade);
    }

    public void Remover(TEntity entidade)
    {
        _dbSet.Remove(entidade);
    }
}
