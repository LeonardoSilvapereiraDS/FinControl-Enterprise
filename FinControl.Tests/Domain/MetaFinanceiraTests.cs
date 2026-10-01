using FinControl.Domain.Entities;
using FinControl.Domain.Exceptions;

namespace FinControl.Tests.Domain;

public sealed class MetaFinanceiraTests
{
    [Fact]
    public void CriarMeta_DeveRejeitarDataLimiteAnteriorADataInicial()
    {
        Assert.Throws<RegraDeNegocioException>(() => new MetaFinanceira(
            "Reserva",
            "Reserva de emergencia",
            valorObjetivo: 5000m,
            valorAtual: 100m,
            dataInicio: new DateTime(2026, 6, 10),
            dataLimite: new DateTime(2026, 6, 1),
            usuarioId: 1));
    }

    [Fact]
    public void AjustarProgresso_DeveRejeitarResultadoNegativo()
    {
        var meta = new MetaFinanceira(
            "Reserva",
            "Reserva de emergencia",
            valorObjetivo: 5000m,
            valorAtual: 100m,
            dataInicio: new DateTime(2026, 6, 1),
            dataLimite: new DateTime(2026, 12, 31),
            usuarioId: 1);

        Assert.Throws<RegraDeNegocioException>(() => meta.AjustarProgresso(-101m));
    }

    [Fact]
    public void PercentualProgresso_DeveCalcularPercentualEConclusao()
    {
        var meta = new MetaFinanceira(
            "Notebook",
            null,
            valorObjetivo: 4000m,
            valorAtual: 1000m,
            dataInicio: new DateTime(2026, 6, 1),
            dataLimite: new DateTime(2026, 12, 31),
            usuarioId: 1);

        Assert.Equal(25m, meta.PercentualProgresso);
        Assert.False(meta.Concluida);

        meta.AtualizarValorAtual(4000m);

        Assert.Equal(100m, meta.PercentualProgresso);
        Assert.True(meta.Concluida);
    }
}
