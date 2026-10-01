using FinControl.Domain.Common;

namespace FinControl.Tests.Application;

internal static class EntidadeTesteHelper
{
    public static TEntity ComId<TEntity>(this TEntity entidade, int id)
        where TEntity : Entidade
    {
        var propriedade = typeof(Entidade).GetProperty(nameof(Entidade.Id));
        var setter = propriedade?.GetSetMethod(nonPublic: true);

        setter?.Invoke(entidade, [id]);

        return entidade;
    }
}
