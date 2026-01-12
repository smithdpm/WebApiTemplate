using Application.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Events.DomainEvents;

namespace Infrastructure.Events;

internal class DomainEventDispatcher(IServiceScopeFactory scopeFactory) : IDomainEventDispatcher
{
    public async Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());

            using (var scope = scopeFactory.CreateScope())
            {
                var handlers = scope.ServiceProvider.GetServices(handlerType);

                foreach (var handler in handlers)
                {
                    if (handler == null) continue;

                    var handlerWrapper = HandlerWrapper.Create(handler, domainEvent.GetType());

                    await handlerWrapper.Handle(domainEvent, cancellationToken);
                }
            }           
        }    
    }

    private abstract class HandlerWrapper
    {
        public abstract Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken);

        public static HandlerWrapper Create(object handler, Type domainEventType)
        {
            Type wrapperType = typeof(HandlerWrapper<>).MakeGenericType(domainEventType);

            var wrapper = Activator.CreateInstance(wrapperType, handler);

            if (wrapper == null)
                throw new Exception($"Creating instance of {wrapperType} returned null value.");

            return (HandlerWrapper)wrapper;
        }
    }

    private class HandlerWrapper<TEvent>(IDomainEventHandler<TEvent> handler): HandlerWrapper where TEvent : IDomainEvent
    {
        public override Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            return handler.HandleAsync((TEvent)domainEvent, cancellationToken);
        }
    }
}

