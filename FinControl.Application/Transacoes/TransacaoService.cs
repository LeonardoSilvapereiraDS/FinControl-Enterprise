using FinControl.Application.Abstractions.Persistence;
using FinControl.Domain.Entities;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;

namespace FinControl.Application.Transacoes;

public sealed class TransacaoService : ITransacaoService
{
    private readonly IRepositorio<Categoria> _categorias;
    private readonly IRepositorio<Conta> _contas;
    private readonly IRepositorio<Transacao> _transacoes;
    private readonly IUnitOfWork _unitOfWork;

    public TransacaoService(
        IRepositorio<Categoria> categorias,
        IRepositorio<Conta> contas,
        IRepositorio<Transacao> transacoes,
        IUnitOfWork unitOfWork)
    {
        _categorias = categorias;
        _contas = contas;
        _transacoes = transacoes;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TransacaoDto>> ListarAsync(
        int usuarioId,
        TransacaoFiltro? filtro = null,
        CancellationToken cancellationToken = default)
    {
        if (filtro?.DataInicial > filtro?.DataFinal)
            throw new RegraDeNegocioException("A data inicial não pode ser posterior à data final.");
        var transacoes = await _transacoes.ListarAsync(cancellationToken);
        var categorias = await _categorias.ListarAsync(cancellationToken);
        var contas = await _contas.ListarAsync(cancellationToken);

        var categoriasPorId = categorias.ToDictionary(categoria => categoria.Id);
        var contasPorId = contas.ToDictionary(conta => conta.Id);

        return transacoes
            .Where(transacao => transacao.UsuarioId == usuarioId)
            .Where(transacao => filtro?.DataInicial is null || transacao.Data.Date >= filtro.DataInicial.Value.Date)
            .Where(transacao => filtro?.DataFinal is null || transacao.Data.Date <= filtro.DataFinal.Value.Date)
            .Where(transacao => filtro?.Tipo is null || transacao.Tipo == filtro.Tipo)
            .Where(transacao => filtro?.CategoriaId is null || transacao.CategoriaId == filtro.CategoriaId)
            .Where(transacao => filtro?.ContaId is null || transacao.ContaId == filtro.ContaId)
            .OrderByDescending(transacao => transacao.Data)
            .ThenBy(transacao => transacao.Descricao)
            .Select(transacao => Mapear(
                transacao,
                categoriasPorId.GetValueOrDefault(transacao.CategoriaId),
                contasPorId.GetValueOrDefault(transacao.ContaId)))
            .ToList();
    }

    public async Task<TransacaoDto> CriarAsync(
        int usuarioId,
        SalvarTransacaoRequest request,
        CancellationToken cancellationToken = default)
    {
        var contexto = await ObterContextoAsync(usuarioId, request, cancellationToken);

        var transacao = new Transacao(
            request.Descricao,
            request.Valor,
            request.Data,
            request.Tipo,
            request.CategoriaId,
            contexto.Categoria.Tipo,
            request.ContaId,
            usuarioId,
            request.Observacao,
            request.Pago,
            request.Recorrente);

        await _transacoes.AdicionarAsync(transacao, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Mapear(transacao, contexto.Categoria, contexto.Conta);
    }

    public async Task<TransacaoDto> AtualizarAsync(
        int usuarioId,
        int transacaoId,
        SalvarTransacaoRequest request,
        CancellationToken cancellationToken = default)
    {
        var transacao = await ObterTransacaoDoUsuarioAsync(usuarioId, transacaoId, cancellationToken);
        var contexto = await ObterContextoAsync(usuarioId, request, cancellationToken, transacao);

        transacao.Atualizar(
            request.Descricao,
            request.Valor,
            request.Data,
            request.Tipo,
            request.CategoriaId,
            contexto.Categoria.Tipo,
            request.ContaId,
            request.Observacao,
            request.Pago,
            request.Recorrente);

        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Mapear(transacao, contexto.Categoria, contexto.Conta);
    }

    public async Task RemoverAsync(int usuarioId, int transacaoId, CancellationToken cancellationToken = default)
    {
        var transacao = await ObterTransacaoDoUsuarioAsync(usuarioId, transacaoId, cancellationToken);

        _transacoes.Remover(transacao);

        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<Transacao> ObterTransacaoDoUsuarioAsync(
        int usuarioId,
        int transacaoId,
        CancellationToken cancellationToken)
    {
        var transacao = await _transacoes.ObterPorIdAsync(transacaoId, cancellationToken);

        if (transacao is null || transacao.UsuarioId != usuarioId)
        {
            throw new RegraDeNegocioException("Transacao nao encontrada.");
        }

        return transacao;
    }

    private async Task<ContextoTransacao> ObterContextoAsync(
        int usuarioId,
        SalvarTransacaoRequest request,
        CancellationToken cancellationToken,
        Transacao? existente = null)
    {
        var categoria = await _categorias.ObterPorIdAsync(request.CategoriaId, cancellationToken);
        var conta = await _contas.ObterPorIdAsync(request.ContaId, cancellationToken);

        if (categoria is null || categoria.UsuarioId != usuarioId || (!categoria.Ativa && existente?.CategoriaId != categoria.Id))
        {
            throw new RegraDeNegocioException("Categoria invalida para a transacao.");
        }

        if (conta is null || conta.UsuarioId != usuarioId || (!conta.Ativa && existente?.ContaId != conta.Id))
        {
            throw new RegraDeNegocioException("Conta invalida para a transacao.");
        }

        return new ContextoTransacao(categoria, conta);
    }

    private static TransacaoDto Mapear(Transacao transacao, Categoria? categoria, Conta? conta)
    {
        return new TransacaoDto(
            transacao.Id,
            transacao.Descricao,
            transacao.Valor,
            transacao.Data,
            transacao.Tipo,
            transacao.CategoriaId,
            categoria?.Nome ?? "Categoria removida",
            transacao.ContaId,
            conta?.Nome ?? "Conta removida",
            transacao.Observacao,
            transacao.Pago,
            transacao.Recorrente);
    }

    private sealed record ContextoTransacao(Categoria Categoria, Conta Conta);
}
