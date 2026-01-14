using SharedKernel.Abstractions;

namespace Application.Behaviours.RepositoryCaching;
public interface IRepositoryCacheInvalidationHandler<T>
    where T : IHasId
{
    Task HandleAsync(List<ChangedEntity<T>> changedEntities, CancellationToken cancellationToken);
}