using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;

namespace FinControl.Tests.Domain;

public sealed class TransacaoTests
{
    public static TheoryData<decimal> ValoresInvalidos => new()
    {
        0m,
        -10m
    };

    [Fact]
    public void CriarTransacao_DeveAceitarDadosValidos()
    {
        var transacao = new Transacao(
            "Salario",
            2500m,
            new DateTime(2026, 6, 5),
            TipoTransacao.Receita,
            categoriaId: 1,
            TipoCategoria.Receita,
            contaId: 1,
            usuarioId: 1);

        Assert.Equal(2500m, transacao.Valor);
        Assert.Equal(2500m, transacao.ValorComSinal);
        Assert.True(transacao.Pago);
    }

    [Theory]
    [MemberData(nameof(ValoresInvalidos))]
    public void CriarTransacao_DeveRejeitarValorMenorOuIgualAZero(decimal valor)
    {
        Assert.Throws<RegraDeNegocioException>(() => new Transacao(
            "Aluguel",
            valor,
            new DateTime(2026, 6, 5),
            TipoTransacao.Despesa,
            categoriaId: 1,
            TipoCategoria.Despesa,
            contaId: 1,
            usuarioId: 1));
    }

    [Fact]
    public void CriarDespesa_DeveRejeitarCategoriaDeReceita()
    {
        Assert.Throws<RegraDeNegocioException>(() => new Transacao(
            "Mercado",
            120m,
            new DateTime(2026, 6, 5),
            TipoTransacao.Despesa,
            categoriaId: 1,
            TipoCategoria.Receita,
            contaId: 1,
            usuarioId: 1));
    }

    [Fact]
    public void ValorComSinal_DeveSerNegativoParaDespesa()
    {
        var transacao = new Transacao(
            "Mercado",
            120m,
            new DateTime(2026, 6, 5),
            TipoTransacao.Despesa,
            categoriaId: 1,
            TipoCategoria.Despesa,
            contaId: 1,
            usuarioId: 1);

        Assert.Equal(-120m, transacao.ValorComSinal);
    }
}
