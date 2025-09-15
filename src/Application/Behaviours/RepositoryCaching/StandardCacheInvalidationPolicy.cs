using Domain;
using SharedKernel.Database;
using SharedKernel.Extensions;

namespace Application.Behaviours.RepositoryCaching;
internal abstract class StandardCacheInvalidationPolicy<T, TId> : ICacheInvalidationPolicy<T, TId>
    where T : Entity<TId>, IAggregateRoot
    where TId : struct, IEquatable<TId>
{
    protected abstract IEnumerable<Func<T, string?>> CacheInvalidationFunctionsByEntity();
    
    public virtual string CacheInvalidationFunction(ChangedEntity<T, TId> changedEntity)
    { 
        return RepositoryCachingHelper.GenerateCacheKey(typeof(T).Name, changedEntity.Id);
    }

    public virtual IEnumerable<string> GetKeysToInvalidate(ChangedEntity<T, TId> changedEntity)
    {
        return GetKeysToInvalidateNonUnique(changedEntity).Distinct();
    }
    protected virtual IEnumerable<string> GetAdditionalKeysToInvalidate(ChangedEntity<T, TId> changedEntity)
    {
        yield break;
    }

    protected IEnumerable<string> GetKeysToInvalidateNonUnique(ChangedEntity<T, TId> changedEntity)
    {
        yield return CacheInvalidationFunction(changedEntity);

        if (changedEntity.Before != null)
        {
            foreach (var keyFunc in CacheInvalidationFunctionsByEntity()
                .SelectNotNull(changedEntity.Before))
                yield return keyFunc;
        }

        if (changedEntity.After != null)
        {
            foreach (var keyFunc in CacheInvalidationFunctionsByEntity()
                .SelectNotNull(changedEntity.After))
                yield return keyFunc;
        }
        
        foreach (var key in GetAdditionalKeysToInvalidate(changedEntity))
            yield return key;
    }
}