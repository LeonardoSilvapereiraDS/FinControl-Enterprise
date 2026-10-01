using FinControl.Domain.Common;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;

namespace FinControl.Domain.Entities;

public sealed class Transacao : Entidade
{
    public const int TamanhoMaximoDescricao = 160;
    public const int TamanhoMaximoObservacao = 500;

    public string Descricao { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }
    public DateTime Data { get; private set; }
    public TipoTransacao Tipo { get; private set; }
    public int CategoriaId { get; private set; }
    public int ContaId { get; private set; }
    public int UsuarioId { get; private set; }
    public string? Observacao { get; private set; }
    public bool Pago { get; private set; }
    public bool Recorrente { get; private set; }
    public DateTime DataCadastro { get; private set; }

    public decimal ValorComSinal => Tipo == TipoTransacao.Receita ? Valor : -Valor;

    private Transacao()
    {
    }

    public Transacao(
        string descricao,
        decimal valor,
        DateTime data,
        TipoTransacao tipo,
        int categoriaId,
        TipoCategoria tipoCategoria,
        int contaId,
        int usuarioId,
        string? observacao = null,
        bool pago = true,
        bool recorrente = false,
        DateTime? dataCadastro = null)
    {
        AplicarDados(descricao, valor, data, tipo, categoriaId, tipoCategoria, contaId, usuarioId, observacao, pago, recorrente);
        DataCadastro = dataCadastro ?? DateTime.UtcNow;
    }

    public void Atualizar(
        string descricao,
        decimal valor,
        DateTime data,
        TipoTransacao tipo,
        int categoriaId,
        TipoCategoria tipoCategoria,
        int contaId,
        string? observacao,
        bool pago,
        bool recorrente)
    {
        AplicarDados(descricao, valor, data, tipo, categoriaId, tipoCategoria, contaId, UsuarioId, observacao, pago, recorrente);
    }

    public void MarcarComoPago()
    {
        Pago = true;
    }

    public void MarcarComoPendente()
    {
        Pago = false;
    }

    private void AplicarDados(
        string descricao,
        decimal valor,
        DateTime data,
        TipoTransacao tipo,
        int categoriaId,
        TipoCategoria tipoCategoria,
        int contaId,
        int usuarioId,
        string? observacao,
        bool pago,
        bool recorrente)
    {
        var descricaoValidada = ValidacaoDominio.TextoObrigatorio(descricao, nameof(Descricao), TamanhoMaximoDescricao);
        var observacaoValidada = ValidacaoDominio.TextoOpcional(observacao, nameof(Observacao), TamanhoMaximoObservacao);
        ValidacaoDominio.EnumValido(tipo, nameof(Tipo));
        ValidacaoDominio.EnumValido(tipoCategoria, nameof(tipoCategoria));
        ValidacaoDominio.DataValida(data, nameof(Data));
        ValidacaoDominio.ValorPositivo(valor, nameof(Valor));
        ValidacaoDominio.IdObrigatorio(categoriaId, nameof(CategoriaId));
        ValidacaoDominio.IdObrigatorio(contaId, nameof(ContaId));
        ValidacaoDominio.IdObrigatorio(usuarioId, nameof(UsuarioId));
        ValidarCategoriaCompativel(tipo, tipoCategoria);

        Descricao = descricaoValidada;
        Valor = valor;
        Data = data.Date;
        Tipo = tipo;
        CategoriaId = categoriaId;
        ContaId = contaId;
        UsuarioId = usuarioId;
        Observacao = observacaoValidada;
        Pago = pago;
        Recorrente = recorrente;
    }

    private static void ValidarCategoriaCompativel(TipoTransacao tipoTransacao, TipoCategoria tipoCategoria)
    {
        if (tipoTransacao == TipoTransacao.Receita && tipoCategoria != TipoCategoria.Receita)
        {
            throw new RegraDeNegocioException("Uma categoria de despesa nao pode ser usada em uma receita.");
        }

        if (tipoTransacao == TipoTransacao.Despesa && tipoCategoria != TipoCategoria.Despesa)
        {
            throw new RegraDeNegocioException("Uma categoria de receita nao pode ser usada em uma despesa.");
        }
    }
}
