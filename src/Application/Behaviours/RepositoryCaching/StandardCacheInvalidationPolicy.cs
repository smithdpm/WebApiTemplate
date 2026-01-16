using SharedKernel.Abstractions;
using SharedKernel.Extensions;

namespace Application.Behaviours.RepositoryCaching;
internal abstract class StandardCacheInvalidationPolicy<T> : ICacheInvalidationPolicy
    where T : IHasId
{
    public Type EntityType => typeof(T);

    protected abstract IEnumerable<Func<T, string?>> CacheInvalidationFunctionsByEntity();
    
    public virtual string CacheInvalidationFunction(ChangedEntity changedEntity)
    { 
        return RepositoryCachingHelper.GenerateCacheKey(typeof(T).Name, changedEntity.Id);
    }

    public virtual IEnumerable<string> GetKeysToInvalidate(ChangedEntity changedEntity)
    {
        return GetKeysToInvalidateNonUnique(changedEntity).Distinct();
    }
    protected virtual IEnumerable<string> GetAdditionalKeysToInvalidate(ChangedEntity changedEntity)
    {
        yield break;
    }

    protected IEnumerable<string> GetKeysToInvalidateNonUnique(ChangedEntity changedEntity)
    {
        yield return CacheInvalidationFunction(changedEntity);

        if (changedEntity.Before != null)
        {
            foreach (var keyFunc in CacheInvalidationFunctionsByEntity()
                .SelectNotNull((T)changedEntity.Before))
                yield return keyFunc;
        }

        if (changedEntity.After != null)
        {
            foreach (var keyFunc in CacheInvalidationFunctionsByEntity()
                .SelectNotNull((T)changedEntity.After))
                yield return keyFunc;
        }
        
        foreach (var key in GetAdditionalKeysToInvalidate(changedEntity))
            yield return key;
    }
}