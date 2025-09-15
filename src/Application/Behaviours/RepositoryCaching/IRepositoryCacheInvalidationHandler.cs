using Domain;
using SharedKernel.Database;
using System.Threading;

namespace Application.Behaviours.RepositoryCaching;
public interface IRepositoryCacheInvalidationHandler<T, TId>
    where T : Entity<TId>, IAggregateRoot
    where TId : struct, IEquatable<TId>
{
    Task HandleAsync(List<ChangedEntity<T, TId>> changedEntities, CancellationToken cancellationToken);
}