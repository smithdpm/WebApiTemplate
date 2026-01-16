using SharedKernel.Abstractions;

namespace Application.Behaviours.RepositoryCaching;

public class InvalidationMap : IInvalidationMap
{
    private Dictionary<Type, Func<object, IEnumerable<string>>> _maps = new();

    public void RegisterMap<T>(Func<ChangedEntity, IEnumerable<string>> map) 
        where T : IHasId
    {
        _maps[typeof(T)] = (entity) => map((ChangedEntity)entity);
    }
    public IEnumerable<string> GetCacheKeysToInvalidate(ChangedEntity changedEntity)
    {
        if (_maps.TryGetValue(changedEntity.EntityType, out var map))
        {
            return map(changedEntity);
        }
        return Enumerable.Empty<string>();
    }
}
