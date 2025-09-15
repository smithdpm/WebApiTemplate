using Domain;
using SharedKernel.Database;

namespace Application.Behaviours.RepositoryCaching;

public class InvalidationMap : IInvalidationMap
{
    private Dictionary<Type, Func<object, IEnumerable<string>>> _maps = new();

    public void RegisterMap<T, TId>(Func<ChangedEntity<T, TId>, IEnumerable<string>> map) 
        where T : Entity<TId>, IAggregateRoot
        where TId : struct, IEquatable<TId>
    {
        _maps[typeof(T)] = (entity) => map((ChangedEntity < T, TId > )entity);
    }
    public IEnumerable<string> GetCacheKeysToInvalidate<T, TId>(ChangedEntity<T, TId> changedEntity)
        where T : Entity<TId>, IAggregateRoot
        where TId : struct, IEquatable<TId>
    {
        if (_maps.TryGetValue(typeof(T), out var map))
        {
            return map(changedEntity);
        }
        return Enumerable.Empty<string>();
    }
}
