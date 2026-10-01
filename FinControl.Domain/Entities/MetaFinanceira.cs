using FinControl.Domain.Common;
using FinControl.Domain.Exceptions;

namespace FinControl.Domain.Entities;

public sealed class MetaFinanceira : Entidade
{
    public const int TamanhoMaximoNome = 120;
    public const int TamanhoMaximoDescricao = 500;

    public string Nome { get; private set; } = string.Empty;
    public string? Descricao { get; private set; }
    public decimal ValorObjetivo { get; private set; }
    public decimal ValorAtual { get; private set; }
    public DateTime DataInicio { get; private set; }
    public DateTime DataLimite { get; private set; }
    public int UsuarioId { get; private set; }
    public bool Concluida { get; private set; }

    public decimal PercentualProgresso => Math.Min(100m, Math.Round(ValorAtual / ValorObjetivo * 100m, 2));

    private MetaFinanceira()
    {
    }

    public MetaFinanceira(
        string nome,
        string? descricao,
        decimal valorObjetivo,
        decimal valorAtual,
        DateTime dataInicio,
        DateTime dataLimite,
        int usuarioId)
    {
        Nome = ValidacaoDominio.TextoObrigatorio(nome, nameof(Nome), TamanhoMaximoNome);
        Descricao = ValidacaoDominio.TextoOpcional(descricao, nameof(Descricao), TamanhoMaximoDescricao);
        ValidacaoDominio.ValorPositivo(valorObjetivo, nameof(ValorObjetivo));
        ValidacaoDominio.ValorNaoNegativo(valorAtual, nameof(ValorAtual));
        ValidacaoDominio.IdObrigatorio(usuarioId, nameof(UsuarioId));
        ValidarPeriodo(dataInicio, dataLimite);

        ValorObjetivo = valorObjetivo;
        ValorAtual = valorAtual;
        DataInicio = dataInicio;
        DataLimite = dataLimite;
        UsuarioId = usuarioId;
        AtualizarConclusao();
    }

    public void AtualizarDados(
        string nome,
        string? descricao,
        decimal valorObjetivo,
        DateTime dataInicio,
        DateTime dataLimite)
    {
        Nome = ValidacaoDominio.TextoObrigatorio(nome, nameof(Nome), TamanhoMaximoNome);
        Descricao = ValidacaoDominio.TextoOpcional(descricao, nameof(Descricao), TamanhoMaximoDescricao);
        ValidacaoDominio.ValorPositivo(valorObjetivo, nameof(ValorObjetivo));
        ValidarPeriodo(dataInicio, dataLimite);

        ValorObjetivo = valorObjetivo;
        DataInicio = dataInicio;
        DataLimite = dataLimite;
        AtualizarConclusao();
    }

    public void AtualizarValorAtual(decimal valorAtual)
    {
        ValidacaoDominio.ValorNaoNegativo(valorAtual, nameof(ValorAtual));
        ValorAtual = valorAtual;
        AtualizarConclusao();
    }

    public void AjustarProgresso(decimal variacao)
    {
        var novoValor = ValorAtual + variacao;

        if (novoValor < 0)
        {
            throw new RegraDeNegocioException("O progresso de uma meta nao pode ficar negativo.");
        }

        ValidacaoDominio.ValorMonetario(novoValor, nameof(ValorAtual));
        ValorAtual = novoValor;
        AtualizarConclusao();
    }

    private void AtualizarConclusao()
    {
        Concluida = ValorAtual >= ValorObjetivo;
    }

    private static void ValidarPeriodo(DateTime dataInicio, DateTime dataLimite)
    {
        ValidacaoDominio.DataValida(dataInicio, nameof(DataInicio));
        ValidacaoDominio.DataValida(dataLimite, nameof(DataLimite));
        if (dataLimite.Date < dataInicio.Date)
        {
            throw new RegraDeNegocioException("A data-limite de uma meta nao pode ser anterior a data inicial.");
        }
    }
}
