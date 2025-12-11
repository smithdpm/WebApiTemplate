
using Application.Abstractions.Events;
using Domain.Cars.Events;
using SharedKernel.Events.DomainEvents;

namespace Application.Cars.IntegrationEvents;
public class CarSoldEventHandler(IIntegrationEventDispatcher integrationEventDispatcher) : IDomainEventHandler<CarSoldEvent>
{
    public async Task HandleAsync(CarSoldEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var integrationEvent = new CarSoldIntegrationEvent(domainEvent.CarId, domainEvent.SoldAt, domainEvent.SoldPrice);
        await integrationEventDispatcher.DispatchEventsAsync(new[] { integrationEvent }, cancellationToken);
    }
}
