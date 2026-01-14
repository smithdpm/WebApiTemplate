
using SharedKernel.Abstractions;

namespace Application.Behaviours.RepositoryCaching;
public interface IInvalidationMap
{
    void RegisterMap<T>(Func<ChangedEntity<T>, IEnumerable<string>> map)
    where T : IHasId;

    IEnumerable<string> GetCacheKeysToInvalidate<T>(ChangedEntity<T> changedEntity)
    where T : IHasId;
}
