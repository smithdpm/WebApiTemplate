using SharedKernel.Events;

namespace Cqrs.Events.DomainEvents;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

