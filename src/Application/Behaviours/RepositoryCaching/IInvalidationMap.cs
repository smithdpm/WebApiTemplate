
using SharedKernel.Abstractions;

namespace Application.Behaviours.RepositoryCaching;
public interface IInvalidationMap
{
    void RegisterMap<T>(Func<ChangedEntity, IEnumerable<string>> map)
    where T : IHasId;

    IEnumerable<string> GetCacheKeysToInvalidate(ChangedEntity changedEntity);
}
