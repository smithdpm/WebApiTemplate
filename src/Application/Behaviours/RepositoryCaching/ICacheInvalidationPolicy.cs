

using Domain;
using SharedKernel.Database;

namespace Application.Behaviours.RepositoryCaching;
public interface ICacheInvalidationPolicy<T,TId> 
    where T : Entity<TId>, IAggregateRoot
    where TId : struct, IEquatable<TId>
{
    IEnumerable<string> GetKeysToInvalidate(ChangedEntity<T, TId> changedEntity);
}
