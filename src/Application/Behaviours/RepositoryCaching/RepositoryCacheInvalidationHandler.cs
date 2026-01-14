using Application.Abstractions.Services;
using SharedKernel.Abstractions;

namespace Application.Behaviours.RepositoryCaching;
public class RepositoryCacheInvalidationHandler<T>(ICacheService cacheService, IInvalidationMap invalidationMap) : IRepositoryCacheInvalidationHandler<T>
    where T : IHasId
{
    private readonly IInvalidationMap _invalidationMap = invalidationMap;
    private readonly ICacheService _cacheService = cacheService;
    public async Task HandleAsync(List<ChangedEntity<T>> changedEntities, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        foreach (var changedEntity in changedEntities)
        {
            tasks.Add(HandleAsync(changedEntity, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    public async Task HandleAsync(ChangedEntity<T> changedEntity, CancellationToken cancellationToken)
    {
        var cacheKeysToInvalidate = _invalidationMap.GetCacheKeysToInvalidate(changedEntity);

        var tasks = cacheKeysToInvalidate.Select(k =>
            _cacheService.RemoveAsync(k, cancellationToken)
        );

        await Task.WhenAll(tasks);
    }
}
