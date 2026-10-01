using FinControl.Application.Abstractions.Persistence;
using FinControl.Domain.Entities;
using FinControl.Domain.Exceptions;

namespace FinControl.Application.Contas;

public sealed class ContaService : IContaService
{
    private readonly IRepositorio<Conta> _contas;
    private readonly IUnitOfWork _unitOfWork;

    public ContaService(IRepositorio<Conta> contas, IUnitOfWork unitOfWork)
    {
        _contas = contas;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ContaDto>> ListarAsync(
        int usuarioId,
        bool incluirInativas = false,
        CancellationToken cancellationToken = default)
    {
        var contas = await _contas.ListarAsync(cancellationToken);

        return contas
            .Where(conta => conta.UsuarioId == usuarioId)
            .Where(conta => incluirInativas || conta.Ativa)
            .OrderBy(conta => conta.Nome)
            .Select(Mapear)
            .ToList();
    }

    public async Task<ContaDto> CriarAsync(
        int usuarioId,
        SalvarContaRequest request,
        CancellationToken cancellationToken = default)
    {
        await GarantirNomeDisponivelAsync(usuarioId, request.Nome, contaAtualId: null, cancellationToken);

        var conta = new Conta(request.Nome, request.TipoConta, request.SaldoInicial, usuarioId);

        await _contas.AdicionarAsync(conta, cancellationToken);
        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Mapear(conta);
    }

    public async Task<ContaDto> AtualizarAsync(
        int usuarioId,
        int contaId,
        SalvarContaRequest request,
        CancellationToken cancellationToken = default)
    {
        var conta = await ObterContaDoUsuarioAsync(usuarioId, contaId, cancellationToken);

        await GarantirNomeDisponivelAsync(usuarioId, request.Nome, contaId, cancellationToken);

        conta.Atualizar(request.Nome, request.TipoConta, request.SaldoInicial);

        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);

        return Mapear(conta);
    }

    public async Task DesativarAsync(int usuarioId, int contaId, CancellationToken cancellationToken = default)
    {
        var conta = await ObterContaDoUsuarioAsync(usuarioId, contaId, cancellationToken);

        conta.Desativar();

        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    public async Task ReativarAsync(int usuarioId, int contaId, CancellationToken cancellationToken = default)
    {
        var conta = await ObterContaDoUsuarioAsync(usuarioId, contaId, cancellationToken);

        conta.Reativar();

        await _unitOfWork.SalvarAlteracoesAsync(cancellationToken);
    }

    private async Task<Conta> ObterContaDoUsuarioAsync(
        int usuarioId,
        int contaId,
        CancellationToken cancellationToken)
    {
        var conta = await _contas.ObterPorIdAsync(contaId, cancellationToken);

        if (conta is null || conta.UsuarioId != usuarioId)
        {
            throw new RegraDeNegocioException("Conta nao encontrada.");
        }

        return conta;
    }

    private async Task GarantirNomeDisponivelAsync(
        int usuarioId,
        string nome,
        int? contaAtualId,
        CancellationToken cancellationToken)
    {
        var contas = await _contas.ListarAsync(cancellationToken);

        var existe = contas.Any(conta =>
            conta.UsuarioId == usuarioId &&
            conta.Id != contaAtualId &&
            string.Equals(conta.Nome, (nome ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));

        if (existe)
        {
            throw new RegraDeNegocioException("Ja existe uma conta com esse nome.");
        }
    }

    private static ContaDto Mapear(Conta conta)
    {
        return new ContaDto(
            conta.Id,
            conta.Nome,
            conta.TipoConta,
            conta.SaldoInicial,
            conta.Ativa);
    }
}
