namespace FinControl.Domain.Exceptions;

public sealed class RegraDeNegocioException : Exception
{
    public RegraDeNegocioException(string mensagem)
        : base(mensagem)
    {
    }
}
