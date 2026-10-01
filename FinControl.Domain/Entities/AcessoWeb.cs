using FinControl.Domain.Common;

namespace FinControl.Domain.Entities;

// O login é separado do dono dos dados: uma pessoa pode participar de vários espaços.
public sealed class AcessoWeb : Entidade
{
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public string SeloSeguranca { get; set; } = Guid.NewGuid().ToString("N");
    public int Tentativas { get; set; }
    public DateTime? BloqueadoAte { get; set; }
}
public sealed class EspacoFinanceiro : Entidade
{
    public string Nome { get; set; } = "";
    public string Perfil { get; set; } = "Pessoal";
    public int UsuarioId { get; set; }
}
public sealed class MembroEspaco : Entidade
{
    public int EspacoId { get; set; }
    public int AcessoId { get; set; }
    public string Papel { get; set; } = "Administrador";
}
public sealed class Recorrencia : Entidade
{
    public int UsuarioId { get; set; }
    public string Descricao { get; set; } = "";
    public decimal Valor { get; set; }
    public int Tipo { get; set; }
    public int CategoriaId { get; set; }
    public int ContaId { get; set; }
    public string Frequencia { get; set; } = "Mensal";
    public DateTime Inicio { get; set; }
    public DateTime? Fim { get; set; }
    public DateTime ProximaData { get; set; }
    public bool Ativa { get; set; } = true;
}
public sealed class OcorrenciaRecorrente : Entidade
{
    public int RecorrenciaId { get; set; }
    public DateTime Data { get; set; }
    // Sem FK: remover um lançamento não deve recriar a mesma ocorrência.
    public int TransacaoId { get; set; }
}
public sealed class RegistroImportado : Entidade
{
    public int UsuarioId { get; set; }
    public string Chave { get; set; } = "";
    public int TransacaoId { get; set; }
}
