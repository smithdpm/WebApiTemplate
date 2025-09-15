using Application.Abstractions.Services;
using Domain;
using SharedKernel.Database;

namespace Application.Behaviours.RepositoryCaching;
public class RepositoryCacheInvalidationHandler<T, TId>(ICacheService cacheService, IInvalidationMap invalidationMap) : IRepositoryCacheInvalidationHandler<T, TId> where TId : struct, IEquatable<TId>
    where T : Entity<TId>, IAggregateRoot

{
    private readonly IInvalidationMap _invalidationMap = invalidationMap;
    private readonly ICacheService _cacheService = cacheService;
    public async Task HandleAsync(List<ChangedEntity<T, TId>> changedEntities, CancellationToken cancellationToken)
    {
        var tasks = new List<Task>();
        foreach (var changedEntity in changedEntities)
        {
            tasks.Add(HandleAsync(changedEntity, cancellationToken));
        }

        await Task.WhenAll(tasks);
    }

    public async Task HandleAsync(ChangedEntity<T, TId> changedEntity, CancellationToken cancellationToken)
    {
        var cacheKeysToInvalidate = _invalidationMap.GetCacheKeysToInvalidate<T, TId>(changedEntity);

        var tasks = cacheKeysToInvalidate.Select(k =>
            _cacheService.RemoveAsync(k, cancellationToken)
        );

        await Task.WhenAll(tasks);
    }

}
