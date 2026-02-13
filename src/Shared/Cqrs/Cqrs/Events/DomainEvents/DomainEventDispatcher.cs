using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharedKernel.Events;
using System.ComponentModel;

namespace Cqrs.Events.DomainEvents;

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
                    .GetServices(typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType()))
                    .Cast<IDomainEventHandler>();

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

