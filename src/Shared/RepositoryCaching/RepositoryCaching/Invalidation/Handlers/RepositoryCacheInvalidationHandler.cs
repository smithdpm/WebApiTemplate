using RepositoryCaching.Cache;
using RepositoryCaching.Invalidation.Maps;

namespace RepositoryCaching.Invalidation.Handlers;
public class RepositoryCacheInvalidationHandler
    (ICacheService cacheService, IInvalidationMap invalidationMap) 
    : IRepositoryCacheInvalidationHandler
{
    private readonly IInvalidationMap _invalidationMap = invalidationMap;
    private readonly ICacheService _cacheService = cacheService;

    public async Task HandleAsync(List<ChangedEntity> changedEntities, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        foreach (var changedEntity in changedEntities)
        {
            tasks.Add(HandleAsync(changedEntity, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    public async Task HandleAsync(ChangedEntity changedEntity, CancellationToken cancellationToken)
    {
        var cacheKeysToInvalidate = _invalidationMap.GetCacheKeysToInvalidate(changedEntity);

        var tasks = cacheKeysToInvalidate.Select(k =>
            _cacheService.RemoveAsync(k, cancellationToken)
        );

        await Task.WhenAll(tasks);
    }
}
