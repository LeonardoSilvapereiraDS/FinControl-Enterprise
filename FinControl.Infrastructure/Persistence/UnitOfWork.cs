using FinControl.Application.Abstractions.Persistence;

namespace FinControl.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly FinControlDbContext _context;

    public UnitOfWork(FinControlDbContext context)
    {
        _context = context;
    }

    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
