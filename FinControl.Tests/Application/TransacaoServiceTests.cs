using FinControl.Application.Transacoes;
using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;

namespace FinControl.Tests.Application;

public sealed class TransacaoServiceTests
{
    [Fact]
    public async Task CriarAsync_DeveAdicionarTransacaoComCategoriaEContaValidas()
    {
        var categorias = new RepositorioEmMemoria<Categoria>(
        [
            new Categoria("Mercado", TipoCategoria.Despesa, usuarioId: 1).ComId(1)
        ]);

        var contas = new RepositorioEmMemoria<Conta>(
        [
            new Conta("Nubank", TipoConta.ContaCorrente, saldoInicial: 0m, usuarioId: 1).ComId(1)
        ]);

        var transacoes = new RepositorioEmMemoria<Transacao>();
        var unitOfWork = new UnitOfWorkFake();
        var service = new TransacaoService(categorias, contas, transacoes, unitOfWork);

        var transacao = await service.CriarAsync(
            usuarioId: 1,
            new SalvarTransacaoRequest(
                "Compra do mes",
                250m,
                new DateTime(2026, 6, 8),
                TipoTransacao.Despesa,
                CategoriaId: 1,
                ContaId: 1,
                Observacao: null,
                Pago: true,
                Recorrente: false));

        var lista = await service.ListarAsync(usuarioId: 1);

        Assert.Equal("Compra do mes", transacao.Descricao);
        Assert.Equal("Mercado", transacao.CategoriaNome);
        Assert.Equal("Nubank", transacao.ContaNome);
        Assert.Single(lista);
        Assert.Equal(1, unitOfWork.QuantidadeSalvamentos);
    }

    [Fact]
    public async Task CriarAsync_DeveRejeitarCategoriaIncompativelComTipo()
    {
        var categorias = new RepositorioEmMemoria<Categoria>(
        [
            new Categoria("Salario", TipoCategoria.Receita, usuarioId: 1).ComId(1)
        ]);

        var contas = new RepositorioEmMemoria<Conta>(
        [
            new Conta("Nubank", TipoConta.ContaCorrente, saldoInicial: 0m, usuarioId: 1).ComId(1)
        ]);

        var service = new TransacaoService(
            categorias,
            contas,
            new RepositorioEmMemoria<Transacao>(),
            new UnitOfWorkFake());

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => service.CriarAsync(
            usuarioId: 1,
            new SalvarTransacaoRequest(
                "Mercado",
                120m,
                new DateTime(2026, 6, 8),
                TipoTransacao.Despesa,
                CategoriaId: 1,
                ContaId: 1,
                Observacao: null,
                Pago: true,
                Recorrente: false)));
    }
}
