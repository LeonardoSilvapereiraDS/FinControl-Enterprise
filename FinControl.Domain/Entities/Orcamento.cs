using FinControl.Domain.Common;
using FinControl.Domain.Enums;
using FinControl.Domain.Exceptions;

namespace FinControl.Domain.Entities;

public sealed class Orcamento : Entidade
{
    public int CategoriaId { get; private set; }
    public int UsuarioId { get; private set; }
    public decimal ValorLimite { get; private set; }
    public int Mes { get; private set; }
    public int Ano { get; private set; }

    private Orcamento()
    {
    }

    public Orcamento(int categoriaId, TipoCategoria tipoCategoria, int usuarioId, decimal valorLimite, int mes, int ano)
    {
        AplicarDados(categoriaId, tipoCategoria, usuarioId, valorLimite, mes, ano);
    }

    public void Atualizar(decimal valorLimite, int mes, int ano)
    {
        ValidacaoDominio.ValorPositivo(valorLimite, nameof(ValorLimite));
        ValidarMesAno(mes, ano);

        ValorLimite = valorLimite;
        Mes = mes;
        Ano = ano;
    }

    private void AplicarDados(int categoriaId, TipoCategoria tipoCategoria, int usuarioId, decimal valorLimite, int mes, int ano)
    {
        ValidacaoDominio.IdObrigatorio(categoriaId, nameof(CategoriaId));
        ValidacaoDominio.IdObrigatorio(usuarioId, nameof(UsuarioId));
        ValidacaoDominio.ValorPositivo(valorLimite, nameof(ValorLimite));
        ValidarCategoriaDespesa(tipoCategoria);
        ValidarMesAno(mes, ano);

        CategoriaId = categoriaId;
        UsuarioId = usuarioId;
        ValorLimite = valorLimite;
        Mes = mes;
        Ano = ano;
    }

    private static void ValidarCategoriaDespesa(TipoCategoria tipoCategoria)
    {
        if (tipoCategoria != TipoCategoria.Despesa)
        {
            throw new RegraDeNegocioException("O orcamento deve ser vinculado a uma categoria de despesa.");
        }
    }

    private static void ValidarMesAno(int mes, int ano)
    {
        if (mes < 1 || mes > 12)
        {
            throw new RegraDeNegocioException("Mes deve estar entre 1 e 12.");
        }

        if (ano < 1900 || ano > 9998)
        {
            throw new RegraDeNegocioException("Ano deve ser valido.");
        }
    }
}
