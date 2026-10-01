using FinControl.Application.Categorias;
using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;

namespace FinControl.Tests.Application;

public sealed class CategoriaServiceTests
{
    [Fact]
    public async Task CriarAsync_DeveAdicionarCategoriaDoUsuario()
    {
        var repositorio = new RepositorioEmMemoria<Categoria>();
        var unitOfWork = new UnitOfWorkFake();
        var service = new CategoriaService(repositorio, unitOfWork, new RepositorioEmMemoria<Transacao>(), new RepositorioEmMemoria<Orcamento>());

        var categoria = await service.CriarAsync(
            usuarioId: 1,
            new SalvarCategoriaRequest("Mercado", TipoCategoria.Despesa));

        var categorias = await service.ListarAsync(usuarioId: 1);

        Assert.Equal("Mercado", categoria.Nome);
        Assert.Single(categorias);
        Assert.Equal(1, unitOfWork.QuantidadeSalvamentos);
    }

    [Fact]
    public async Task CriarAsync_DeveRejeitarNomeDuplicadoNoMesmoTipo()
    {
        var repositorio = new RepositorioEmMemoria<Categoria>(
        [
            new Categoria("Mercado", TipoCategoria.Despesa, usuarioId: 1)
        ]);

        var service = new CategoriaService(repositorio, new UnitOfWorkFake(), new RepositorioEmMemoria<Transacao>(), new RepositorioEmMemoria<Orcamento>());

        await Assert.ThrowsAsync<RegraDeNegocioException>(() => service.CriarAsync(
            usuarioId: 1,
            new SalvarCategoriaRequest("mercado", TipoCategoria.Despesa)));
    }
}
