using FinControl.Domain.Common;
using FinControl.Domain.Enums;

namespace FinControl.Domain.Entities;

public sealed class Categoria : Entidade
{
    public const int TamanhoMaximoNome = 100;

    public string Nome { get; private set; } = string.Empty;
    public TipoCategoria Tipo { get; private set; }
    public int UsuarioId { get; private set; }
    public bool Ativa { get; private set; }

    private Categoria()
    {
    }

    public Categoria(string nome, TipoCategoria tipo, int usuarioId)
    {
        ValidacaoDominio.EnumValido(tipo, nameof(tipo));
        Nome = ValidacaoDominio.TextoObrigatorio(nome, nameof(Nome), TamanhoMaximoNome);
        Tipo = tipo;
        ValidacaoDominio.IdObrigatorio(usuarioId, nameof(UsuarioId));
        UsuarioId = usuarioId;
        Ativa = true;
    }

    public void Atualizar(string nome, TipoCategoria tipo)
    {
        ValidacaoDominio.EnumValido(tipo, nameof(tipo));
        Nome = ValidacaoDominio.TextoObrigatorio(nome, nameof(Nome), TamanhoMaximoNome);
        Tipo = tipo;
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
