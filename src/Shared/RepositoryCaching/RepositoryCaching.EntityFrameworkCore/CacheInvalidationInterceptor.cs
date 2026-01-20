using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RepositoryCaching.Invalidation;
using RepositoryCaching.Invalidation.Handlers;
using SharedKernel.Abstractions;
using SharedKernel.Database;
using System.Runtime.CompilerServices;

namespace RepositoryCaching.EntityFrameworkCore;
internal class CacheInvalidationInterceptor(IRepositoryCacheInvalidationHandler repositoryCacheInvalidationHandler) : SaveChangesInterceptor
{
    private readonly ConditionalWeakTable<DbContext, List<ChangedEntity>> _changedEntities = new();
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var changedEntities = GetChangedEntities(context);

        _changedEntities.Add(context, changedEntities);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        if (_changedEntities.TryGetValue(context, out var entities))
        {
            await repositoryCacheInvalidationHandler.HandleAsync(entities, cancellationToken);
            _changedEntities.Remove(context);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context != null)
        {
            _changedEntities.Remove(context);
        }
        
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private List<ChangedEntity> GetChangedEntities(DbContext dbContext)
    {
        return dbContext.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAggregateRoot && e.Entity is IHasId &&
                (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .Select(e=> GetChangedEntityFromEntityEntry(e, ((IHasId)e.Entity).GetId()))
            .ToList();
    }

    private ChangedEntity GetChangedEntityFromEntityEntry(EntityEntry entityEntry, string entityId)
    {
        Type entityType = entityEntry.Entity.GetType();
        object? before = default;
        object? after = default;

        switch (entityEntry.State)
        {
            case EntityState.Added:
                after = entityEntry.Entity;
                break;

            case EntityState.Modified:
                before = entityEntry.OriginalValues.ToObject();
                after = entityEntry.Entity;
                break;

            case EntityState.Deleted:
                before = entityEntry.OriginalValues.ToObject();
                break;
        }

        return new ChangedEntity(entityId, entityType, before, after);
    }
}