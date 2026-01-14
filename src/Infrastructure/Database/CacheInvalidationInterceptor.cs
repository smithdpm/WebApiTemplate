using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedKernel.Database;

namespace Infrastructure.Database;
internal class CacheInvalidationInterceptor(CacheInvalidatorGenericHandler cacheInvalidatorGenericHandler) : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is null)
            return result;

        var changedEntities = GetChangedEntities(context);

        await cacheInvalidatorGenericHandler.HandleCacheInvalidationAsync(changedEntities, cancellationToken);

        return result;
    }

    private List<EntityEntry> GetChangedEntities(DbContext dbContext)
    {
        return dbContext.ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAggregateRoot &&
                (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted))
            .ToList();
    }
}

