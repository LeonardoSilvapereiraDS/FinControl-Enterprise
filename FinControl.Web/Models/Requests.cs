using FinControl.Domain.Enums;
namespace FinControl.Web.Models;

public sealed record CadastroRequest(string Nome, string Email, string Senha, string Perfil, string Espaco, string? CodigoInicial);
public sealed record LoginRequest(string Email, string Senha);
public sealed record SenhaRequest(string SenhaAtual, string NovaSenha);
public sealed record EspacoRequest(string Nome, string Perfil);
public sealed record MembroRequest(string Email, string Papel);
public sealed record SituacaoRequest(bool Ativa);
public sealed record PagoRequest(bool Pago);
public sealed record MetaRequest(string Nome, string? Descricao, decimal ValorObjetivo, decimal ValorAtual, DateTime DataInicio, DateTime DataLimite);
public sealed record OrcamentoRequest(int CategoriaId, decimal ValorLimite, int Mes, int Ano);
public sealed record RecorrenciaRequest(string Descricao, decimal Valor, TipoTransacao Tipo, int CategoriaId, int ContaId, string Frequencia, DateTime Inicio, DateTime? Fim);
public sealed record ImportarRequest(string Conteudo, string Formato, int ContaId, int CategoriaReceitaId, int CategoriaDespesaId, bool Confirmar);
