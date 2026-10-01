using FinControl.Application.Abstractions.Persistence;
using FinControl.Domain.Entities;
using FinControl.Domain.Exceptions;

namespace FinControl.Application.Categorias;

public sealed class CategoriaService : ICategoriaService
{
    private readonly IRepositorio<Categoria> _categorias;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepositorio<Transacao> _transacoes;
    private readonly IRepositorio<Orcamento> _orcamentos;

    public CategoriaService(IRepositorio<Categoria> categorias, IUnitOfWork unitOfWork, IRepositorio<Transacao> transacoes, IRepositorio<Orcamento> orcamentos)
    {
        _categorias = categorias;
        _unitOfWork = unitOfWork;
        _transacoes = transacoes;
        _orcamentos = orcamentos;
    }

    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(
        int usuarioId,
        bool incluirInativas = false,
        CancellationToken cancellationToken = default)
    {
        var categorias = await _categorias.ListarAsync(cancellationToken);

        return categorias
            .Where(categoria => categoria.UsuarioId == usuarioId)
            .Where(categoria => incluirInativas || categoria.Ativa)
            .OrderBy(categoria => categoria.Tipo)
            .ThenBy(categoria => categoria.Nome)
            .Select(Mapear)
            .ToList();
    }

    public async Task<CategoriaDto> CriarAsync(
        int usuarioId,
        SalvarCategoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        await GarantirNomeDisponivelAsync(usuarioId, request, categoriaAtualId: null, cancellationToken);

        var categoria = new Categoria(request.Nome, request.Tipo, usuarioId);

        await _categorias.AdicionarAsync(categoria, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Mapear(categoria);
    }

    public async Task<CategoriaDto> AtualizarAsync(
        int usuarioId,
        int categoriaId,
        SalvarCategoriaRequest request,
        CancellationToken cancellationToken = default)
    {
        var categoria = await ObterCategoriaDoUsuarioAsync(usuarioId, categoriaId, cancellationToken);

        await GarantirNomeDisponivelAsync(usuarioId, request, categoriaId, cancellationToken);

        if (categoria.Tipo != request.Tipo &&
            ((await _transacoes.ListarAsync(cancellationToken)).Any(t => t.CategoriaId == categoriaId) ||
             (await _orcamentos.ListarAsync(cancellationToken)).Any(o => o.CategoriaId == categoriaId)))
            throw new RegraDeNegocioException("O tipo de uma categoria utilizada não pode ser alterado. Crie uma nova categoria.");
        categoria.Atualizar(request.Nome, request.Tipo);

        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Mapear(categoria);
    }

    public async Task DesativarAsync(int usuarioId, int categoriaId, CancellationToken cancellationToken = default)
    {
        var categoria = await ObterCategoriaDoUsuarioAsync(usuarioId, categoriaId, cancellationToken);

        categoria.Desativar();

        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task ReativarAsync(int usuarioId, int categoriaId, CancellationToken cancellationToken = default)
    {
        var categoria = await ObterCategoriaDoUsuarioAsync(usuarioId, categoriaId, cancellationToken);

        categoria.Reativar();

        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<Categoria> ObterCategoriaDoUsuarioAsync(
        int usuarioId,
        int categoriaId,
        CancellationToken cancellationToken)
    {
        var categoria = await _categorias.ObterPorIdAsync(categoriaId, cancellationToken);

        if (categoria is null || categoria.UsuarioId != usuarioId)
        {
            throw new RegraDeNegocioException("Categoria nao encontrada.");
        }

        return categoria;
    }

    private async Task GarantirNomeDisponivelAsync(
        int usuarioId,
        SalvarCategoriaRequest request,
        int? categoriaAtualId,
        CancellationToken cancellationToken)
    {
        var categorias = await _categorias.ListarAsync(cancellationToken);

        var existe = categorias.Any(categoria =>
            categoria.UsuarioId == usuarioId &&
            categoria.Id != categoriaAtualId &&
            categoria.Tipo == request.Tipo &&
            string.Equals(categoria.Nome, (request.Nome ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));

        if (existe)
        {
            throw new RegraDeNegocioException("Ja existe uma categoria com esse nome e tipo.");
        }
    }

    private static CategoriaDto Mapear(Categoria categoria)
    {
        return new CategoriaDto(
            categoria.Id,
            categoria.Nome,
            categoria.Tipo,
            categoria.Ativa);
    }
}
