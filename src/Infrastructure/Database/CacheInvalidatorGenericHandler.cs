using Application.Behaviours.RepositoryCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions;

namespace Infrastructure.Database;

internal class CacheInvalidatorGenericHandler(IServiceScopeFactory scopeFactory)
{
    public async Task HandleCacheInvalidationAsync(IEnumerable<EntityEntry> changedEntities, CancellationToken cancellationToken = default)
    {
        foreach (var changedEntityGroup in changedEntities.GroupBy(g => g.Entity.GetType(), g => g,
            (entityType, changedEntity) => new {
                Key = entityType,
                ChangedEntities = changedEntity.ToList()
            }))
        {
            var handlerType = typeof(IRepositoryCacheInvalidationHandler<>).MakeGenericType(changedEntityGroup.Key);

            using (var scope = scopeFactory.CreateScope())
            {
                var handlers = scope.ServiceProvider.GetServices(handlerType);

                foreach (var handler in handlers)
                {
                    if (handler == null) continue;

                    var handlerWrapper = HandlerWrapper.Create(handler, changedEntityGroup.Key);

                    await handlerWrapper.Handle(changedEntityGroup.ChangedEntities, cancellationToken);
                }
            }
        }
    }
    private abstract class HandlerWrapper
    {
        public abstract Task Handle(IEnumerable<EntityEntry> changedEntities, CancellationToken cancellationToken);

        public static HandlerWrapper Create(object handler, Type entityType)
        {
            Type wrapperType = typeof(HandlerWrapper<>).MakeGenericType(entityType);

            var wrapper = Activator.CreateInstance(wrapperType, handler);

            if (wrapper == null)
                throw new Exception($"Creating instance of {wrapperType} returned null value.");

            return (HandlerWrapper)wrapper;
        }
    }

    private class HandlerWrapper<T>(IRepositoryCacheInvalidationHandler<T> handler) : HandlerWrapper
        where T : IHasId
    {
        public override async Task Handle(IEnumerable<EntityEntry> changedEntities, CancellationToken cancellationToken)
        {
            var typedChangedEntities = changedEntities
                .Select(ce => GetChangedEntityFromEntityEntry(ce))
                .ToList();

            await handler.HandleAsync(typedChangedEntities, cancellationToken);
        }

        private ChangedEntity<T> GetChangedEntityFromEntityEntry(EntityEntry entityEntry)
        {
            Type entityType = entityEntry.Entity.GetType();
            T? before = default;
            T? after = default;

            switch (entityEntry.State)
            {
                case EntityState.Added:
                    after = (T)entityEntry.Entity;
                    break;

                case EntityState.Modified:
                    before = (T)entityEntry.OriginalValues.ToObject();
                    after = (T)entityEntry.Entity;
                    break;

                case EntityState.Deleted:
                    before = (T)entityEntry.OriginalValues.ToObject();
                    break;
            }
            string entityId = (after?.GetId() ?? before?.GetId())!;
            return new ChangedEntity<T>(entityId, before, after);
        }
    }
}
