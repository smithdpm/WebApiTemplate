using SharedKernel.Events.DomainEvents;

namespace Application.Abstractions.Events;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

