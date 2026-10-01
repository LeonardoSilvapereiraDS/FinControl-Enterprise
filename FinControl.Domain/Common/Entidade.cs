using FinControl.Domain.Interfaces;

namespace FinControl.Domain.Common;

public abstract class Entidade : IEntidade
{
    public int Id { get; protected set; }
}
