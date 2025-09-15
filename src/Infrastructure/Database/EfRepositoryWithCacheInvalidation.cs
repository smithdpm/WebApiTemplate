using Application.Abstractions.Events;
using Application.Behaviours.RepositoryCaching;
using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedKernel.Database;


namespace Infrastructure.Database;
public sealed class EfRepositoryWithCacheInvalidation<T, TId>: EfRepository<T> 
    where TId : struct, IEquatable<TId>
    where T : Entity<TId>, IAggregateRoot
{
    private readonly IRepositoryCacheInvalidationHandler<T, TId> _repositoryCacheInvalidationHandler;
    public EfRepositoryWithCacheInvalidation(CatalogContext dbContext
        , IDomainEventDispatcher domainEventDispatcher
        , IRepositoryCacheInvalidationHandler<T, TId> repositoryCacheInvalidationHandler)
        : base(dbContext, domainEventDispatcher)
    {
        _repositoryCacheInvalidationHandler = repositoryCacheInvalidationHandler;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var changedEntities = GetChangedEntities();

        var result = await base.SaveChangesAsync(cancellationToken);

        await _repositoryCacheInvalidationHandler.HandleAsync(changedEntities, cancellationToken);

        return result;
    }

    private List<ChangedEntity<T, TId>> GetChangedEntities()
    {
        var updatedEntries = _dbContext.ChangeTracker
            .Entries<Entity<TId>>()
            .Where(e =>
                (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();

        return updatedEntries.Select(e => GetChangedEntityFromEntityEntry(e)).ToList();
    }

    private ChangedEntity<T, TId> GetChangedEntityFromEntityEntry(EntityEntry<Entity<TId>> entityEntry)
    {
        T? before = null;
        T? after = null;

        switch (entityEntry.State)
        {
            case EntityState.Added:
                after = entityEntry.Entity as T;
                break;

            case EntityState.Modified:
                before = entityEntry.OriginalValues.ToObject() as T;
                after = entityEntry.Entity as T;
                break;

            case EntityState.Deleted:
                before = entityEntry.OriginalValues.ToObject() as T;
                break;
        }

        return new ChangedEntity<T, TId>(entityEntry.Entity.Id, entityEntry.Entity.GetType(), before, after);
    }
}
