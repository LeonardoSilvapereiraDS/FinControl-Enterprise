using System.Net.Mail;
using FinControl.Domain.Exceptions;

namespace FinControl.Domain.Common;

internal static class ValidacaoDominio
{
    public static string TextoObrigatorio(string? valor, string campo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new RegraDeNegocioException($"{campo} e obrigatorio.");
        }

        var texto = valor.Trim();

        if (texto.Length > tamanhoMaximo)
        {
            throw new RegraDeNegocioException($"{campo} deve ter no maximo {tamanhoMaximo} caracteres.");
        }

        return texto;
    }

    public static string? TextoOpcional(string? valor, string campo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var texto = valor.Trim();

        if (texto.Length > tamanhoMaximo)
        {
            throw new RegraDeNegocioException($"{campo} deve ter no maximo {tamanhoMaximo} caracteres.");
        }

        return texto;
    }

    public static string EmailObrigatorio(string? valor, string campo, int tamanhoMaximo)
    {
        var email = TextoObrigatorio(valor, campo, tamanhoMaximo).ToLowerInvariant();

        try
        {
            var endereco = new MailAddress(email);

            if (!string.Equals(endereco.Address, email, StringComparison.OrdinalIgnoreCase))
            {
                throw new RegraDeNegocioException($"{campo} deve ser valido.");
            }
        }
        catch (FormatException)
        {
            throw new RegraDeNegocioException($"{campo} deve ser valido.");
        }

        return email;
    }

    public static void IdObrigatorio(int valor, string campo)
    {
        ValorMonetario(valor, campo);
        if (valor <= 0)
        {
            throw new RegraDeNegocioException($"{campo} deve ser informado.");
        }
    }

    public static void ValorPositivo(decimal valor, string campo)
    {
        ValorMonetario(valor, campo);
        if (valor <= 0)
        {
            throw new RegraDeNegocioException($"{campo} deve ser maior que zero.");
        }
    }

    public static void ValorNaoNegativo(decimal valor, string campo)
    {
        ValorMonetario(valor, campo);
        if (valor < 0)
        {
            throw new RegraDeNegocioException($"{campo} nao pode ser negativo.");
        }
    }
    public static void EnumValido<T>(T valor, string campo) where T : struct, Enum
    {
        if (!Enum.IsDefined(valor))
            throw new RegraDeNegocioException($"{campo} inválido.");
    }

    public static void ValorMonetario(decimal valor, string campo)
    {
        if ((valor > 999999999999.99m || valor < -999999999999.99m) || decimal.Round(valor, 2) != valor)
            throw new RegraDeNegocioException($"{campo} deve ter até duas casas decimais e ser menor que 1 trilhão.");
    }

    public static void DataValida(DateTime valor, string campo)
    {
        if (valor.Year < 1900 || valor.Year > 9998)
            throw new RegraDeNegocioException($"{campo} deve estar entre os anos 1900 e 9998.");
    }
}
