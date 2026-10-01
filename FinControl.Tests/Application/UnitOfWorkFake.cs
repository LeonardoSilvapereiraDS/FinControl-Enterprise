using FinControl.Application.Abstractions.Persistence;

namespace FinControl.Tests.Application;

internal sealed class UnitOfWorkFake : IUnitOfWork
{
    public int QuantidadeSalvamentos { get; private set; }

    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        QuantidadeSalvamentos++;

        return Task.FromResult(1);
    }
}
