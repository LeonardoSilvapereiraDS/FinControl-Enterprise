using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;

namespace FinControl.Tests.Domain;

public sealed class OrcamentoTests
{
    [Fact]
    public void CriarOrcamento_DeveAceitarCategoriaDeDespesa()
    {
        var orcamento = new Orcamento(
            categoriaId: 1,
            TipoCategoria.Despesa,
            usuarioId: 1,
            valorLimite: 900m,
            mes: 6,
            ano: 2026);

        Assert.Equal(900m, orcamento.ValorLimite);
        Assert.Equal(6, orcamento.Mes);
        Assert.Equal(2026, orcamento.Ano);
    }

    [Fact]
    public void CriarOrcamento_DeveRejeitarCategoriaDeReceita()
    {
        Assert.Throws<RegraDeNegocioException>(() => new Orcamento(
            categoriaId: 1,
            TipoCategoria.Receita,
            usuarioId: 1,
            valorLimite: 900m,
            mes: 6,
            ano: 2026));
    }
}
