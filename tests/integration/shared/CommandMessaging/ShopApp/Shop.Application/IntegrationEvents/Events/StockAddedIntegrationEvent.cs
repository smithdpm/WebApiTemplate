using Cqrs.Events.IntegrationEvents;

namespace Shop.Application.IntegrationEvents.Events;

public record StockAddedIntegrationEvent
    (string ProductName, int QuantityAdded)
: IntegrationEventBase;