using FinControl.Domain.Common;
using FinControl.Domain.Enums;

namespace FinControl.Domain.Entities;

public sealed class Conta : Entidade
{
    public const int TamanhoMaximoNome = 100;

    public string Nome { get; private set; } = string.Empty;
    public TipoConta TipoConta { get; private set; }
    public decimal SaldoInicial { get; private set; }
    public int UsuarioId { get; private set; }
    public bool Ativa { get; private set; }

    private Conta()
    {
    }

    public Conta(string nome, TipoConta tipoConta, decimal saldoInicial, int usuarioId)
    {
        ValidacaoDominio.EnumValido(tipoConta, nameof(tipoConta));
        ValidacaoDominio.ValorMonetario(saldoInicial, nameof(SaldoInicial));
        Nome = ValidacaoDominio.TextoObrigatorio(nome, nameof(Nome), TamanhoMaximoNome);
        TipoConta = tipoConta;
        SaldoInicial = saldoInicial;
        ValidacaoDominio.IdObrigatorio(usuarioId, nameof(UsuarioId));
        UsuarioId = usuarioId;
        Ativa = true;
    }

    public void Atualizar(string nome, TipoConta tipoConta, decimal saldoInicial)
    {
        ValidacaoDominio.EnumValido(tipoConta, nameof(tipoConta));
        ValidacaoDominio.ValorMonetario(saldoInicial, nameof(SaldoInicial));
        Nome = ValidacaoDominio.TextoObrigatorio(nome, nameof(Nome), TamanhoMaximoNome);
        TipoConta = tipoConta;
        SaldoInicial = saldoInicial;
    }

    public void Desativar()
    {
        Ativa = false;
    }

    public void Reativar()
    {
        Ativa = true;
    }
}
