namespace FinControl.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
