using FinControl.Domain.Common;

namespace FinControl.Domain.Entities;

public sealed class Usuario : Entidade
{
    public const int TamanhoMaximoNome = 120;
    public const int TamanhoMaximoEmail = 180;
    public const int TamanhoMaximoSenhaHash = 255;

    public string Nome { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string SenhaHash { get; private set; } = string.Empty;
    public DateTime DataCadastro { get; private set; }
    public bool Ativo { get; private set; }

    private Usuario()
    {
    }

    public Usuario(string nome, string email, string senhaHash, DateTime? dataCadastro = null)
    {
        Nome = ValidacaoDominio.TextoObrigatorio(nome, nameof(Nome), TamanhoMaximoNome);
        Email = ValidacaoDominio.EmailObrigatorio(email, nameof(Email), TamanhoMaximoEmail);
        SenhaHash = ValidacaoDominio.TextoObrigatorio(senhaHash, nameof(SenhaHash), TamanhoMaximoSenhaHash);
        DataCadastro = dataCadastro ?? DateTime.UtcNow;
        Ativo = true;
    }

    public void AtualizarPerfil(string nome, string email)
    {
        Nome = ValidacaoDominio.TextoObrigatorio(nome, nameof(Nome), TamanhoMaximoNome);
        Email = ValidacaoDominio.EmailObrigatorio(email, nameof(Email), TamanhoMaximoEmail);
    }

    public void AtualizarSenhaHash(string senhaHash)
    {
        SenhaHash = ValidacaoDominio.TextoObrigatorio(senhaHash, nameof(SenhaHash), TamanhoMaximoSenhaHash);
    }

    public void Desativar()
    {
        Ativo = false;
    }

    public void Reativar()
    {
        Ativo = true;
    }
}
