using Ardalis.Specification;
using Domain;
using SharedKernel.Database;

namespace Application.Behaviours.RepositoryCaching;
public interface IInvalidationMap
{
    void RegisterMap<T, TId>(Func<ChangedEntity<T, TId>, IEnumerable<string>> map)
        where T : Entity<TId>, IAggregateRoot
        where TId : struct, IEquatable<TId>;

    IEnumerable<string> GetCacheKeysToInvalidate<T, TId>(ChangedEntity<T, TId> changedEntity) where T : Entity<TId>, IAggregateRoot
        where TId : struct, IEquatable<TId>;
}
