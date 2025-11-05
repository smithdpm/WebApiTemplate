using SharedKernel.Events.IntegrationEvents;

namespace Application.Abstractions.Events;
public interface IIntegrationEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IIntegrationEvent> integrationEvents, CancellationToken cancellationToken = default);
}
