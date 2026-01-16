namespace Application.Behaviours.RepositoryCaching;
public interface IRepositoryCacheInvalidationHandler
{
    Task HandleAsync(List<ChangedEntity> changedEntities, CancellationToken cancellationToken);
}