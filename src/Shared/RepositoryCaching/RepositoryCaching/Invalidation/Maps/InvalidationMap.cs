using RepositoryCaching.Invalidation.Policies;

namespace RepositoryCaching.Invalidation.Maps;

public class InvalidationMap: IInvalidationMap
{
    private Dictionary<Type, Func<object, IEnumerable<string>>> _maps = new();

    public InvalidationMap(IEnumerable<ICacheInvalidationPolicy> invalidationPolicies)
    {
        RegisterInvalidationPolicies(invalidationPolicies);
    }

    private void RegisterInvalidationPolicies(IEnumerable<ICacheInvalidationPolicy> invalidationPolicies)
    {
        var policies = invalidationPolicies
            .GroupBy(p => p.EntityType); 

        foreach (var policyGroup in policies)
        {
            Func<ChangedEntity, IEnumerable<string>> combinedFunc = changedEntity =>
            policyGroup.SelectMany(p => p.GetKeysToInvalidate(changedEntity));

            RegisterMap(policyGroup.Key, combinedFunc);
        }
    }

    public IEnumerable<string> GetCacheKeysToInvalidate(ChangedEntity changedEntity)
    {
        if (_maps.TryGetValue(changedEntity.EntityType, out var map))
        {
            return map(changedEntity);
        }
        return Enumerable.Empty<string>();
    }
    private void RegisterMap(Type entityType, Func<ChangedEntity, IEnumerable<string>> map)
    {
        _maps[entityType] = (entity) => map((ChangedEntity)entity);
    }
}
