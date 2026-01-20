using Cqrs.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using Cqrs.Events.DomainEvents;
using SharedKernel.Events;

namespace Cqrs.Events;

internal class DomainEventDispatcher(IServiceScopeFactory scopeFactory) : IDomainEventDispatcher
{
    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var handlers = scope.ServiceProvider
                    .GetServices<IDomainEventHandler>()
                    .Where(s=>s.EventType == domainEvent.GetType());

                foreach (var handler in handlers)   
                {
                    if (handler == null) continue;

                    await handler.HandleAsync(domainEvent, cancellationToken);
                }
            }           
        }    
    }
}

