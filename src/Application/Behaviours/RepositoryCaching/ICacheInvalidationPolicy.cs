

using SharedKernel.Abstractions;

namespace Application.Behaviours.RepositoryCaching;
public interface ICacheInvalidationPolicy<T> 
    where T : IHasId
{
    IEnumerable<string> GetKeysToInvalidate(ChangedEntity<T> changedEntity);
}
