using Cqrs.Events.IntegrationEvents;

namespace Product.App.Events;

public record StockAddedIntegrationEvent
    (string ProductName, int QuantityAdded)
: IntegrationEventBase;