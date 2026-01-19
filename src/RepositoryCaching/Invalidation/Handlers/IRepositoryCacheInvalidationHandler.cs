namespace RepositoryCaching.Invalidation.Handlers;
public interface IRepositoryCacheInvalidationHandler
{
    Task HandleAsync(List<ChangedEntity> changedEntities, CancellationToken cancellationToken);
}