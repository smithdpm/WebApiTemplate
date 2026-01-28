using Cqrs.Abstractions.Events;
using Cqrs.Events.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;
using System.ComponentModel;

namespace Cqrs.Events;

[EditorBrowsable(EditorBrowsableState.Never)]
public class DomainEventDispatcher(
    IServiceScopeFactory scopeFactory
    ,ILogger<DomainEventDispatcher> logger) : IDomainEventDispatcher
{
    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var handlers = scope.ServiceProvider
                    .GetServices<IDomainEventHandler>()
                    .Where(s=>s.EventType == domainEvent.GetType())
                    .ToList();

                if (!handlers.Any())
                {
                    logger.LogWarning("No handlers registered for domain event of type {DomainEventType}", domainEvent.GetType().FullName);
                    continue;
                }

                foreach (var handler in handlers)   
                {
                    await handler.HandleAsync(domainEvent, cancellationToken);
                }
            }           
        }    
    }
}

