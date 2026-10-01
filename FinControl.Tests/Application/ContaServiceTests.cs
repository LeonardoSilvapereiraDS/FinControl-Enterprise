using FinControl.Application.Contas;
using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;

namespace FinControl.Tests.Application;

public sealed class ContaServiceTests
{
    [Fact]
    public async Task CriarAsync_DeveAdicionarContaDoUsuario()
    {
        var repositorio = new RepositorioEmMemoria<Conta>();
        var unitOfWork = new UnitOfWorkFake();
        var service = new ContaService(repositorio, unitOfWork);

        var conta = await service.CriarAsync(
            usuarioId: 1,
            new SalvarContaRequest("Nubank", TipoConta.ContaCorrente, SaldoInicial: 250m));

        var contas = await service.ListarAsync(usuarioId: 1);

        Assert.Equal("Nubank", conta.Nome);
        Assert.Equal(250m, conta.SaldoInicial);
        Assert.Single(contas);
        Assert.Equal(1, unitOfWork.QuantidadeSalvamentos);
    }

    [Fact]
    public async Task CriarAsync_DeveRejeitarNomeDuplicadoParaUsuario()
    {
        var repositorio = new RepositorioEmMemoria<Conta>(
        [
            new Conta("Nubank", TipoConta.ContaCorrente, saldoInicial: 0m, usuarioId: 1)
        ]);

        var service = new ContaService(repositorio, new UnitOfWorkFake());

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => service.CriarAsync(
            usuarioId: 1,
            new SalvarContaRequest("nubank", TipoConta.Poupanca, SaldoInicial: 10m)));
    }
}
