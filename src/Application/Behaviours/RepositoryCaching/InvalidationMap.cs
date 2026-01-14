using SharedKernel.Abstractions;

namespace Application.Behaviours.RepositoryCaching;

public class InvalidationMap : IInvalidationMap
{
    private Dictionary<Type, Func<object, IEnumerable<string>>> _maps = new();

    public void RegisterMap<T>(Func<ChangedEntity<T>, IEnumerable<string>> map) 
        where T : IHasId
    {
        _maps[typeof(T)] = (entity) => map((ChangedEntity<T>)entity);
    }
    public IEnumerable<string> GetCacheKeysToInvalidate<T>(ChangedEntity<T> changedEntity)
       where T : IHasId
    {
        if (_maps.TryGetValue(typeof(T), out var map))
        {
            return map(changedEntity);
        }
        return Enumerable.Empty<string>();
    }
}
