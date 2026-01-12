
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedKernel.Events;
using SharedKernel.Events.DomainEvents;

namespace Infrastructure.Database;
public class OutboxSaveChangesInterceptor: SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is null)
            return result;

        var domainEvents = GetDomainEvents(context);
        var outboxMessages = DomainEventsToOutboxMessages(domainEvents);

        if (outboxMessages.Any())
            await context.Set<OutboxMessage>().AddRangeAsync(outboxMessages, cancellationToken);

        return result;
    }

    private List<OutboxMessage> DomainEventsToOutboxMessages(List<DomainEventBase> domainEvents)
    {
        var outboxMessages = new List<OutboxMessage>();

        foreach (var domainEvent in domainEvents)
        {
            outboxMessages.Add(new OutboxMessage(
                eventType: domainEvent.GetType().Name ?? string.Empty,
                payload: System.Text.Json.JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                occurredOnUtc: domainEvent.Timestamp
            ));
        }

        return outboxMessages;
    }

    private List<DomainEventBase> GetDomainEvents(DbContext dbContext)
    {
        return dbContext.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entity => entity.Entity)
            .SelectMany(entity =>
            {
                var events = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return events;
            }).ToList();
    }
}
