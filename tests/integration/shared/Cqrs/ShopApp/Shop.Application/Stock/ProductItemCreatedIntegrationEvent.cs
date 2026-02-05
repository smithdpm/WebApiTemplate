using Cqrs.Events.IntegrationEvents;

namespace Shop.Application.Stock;

public record ProductItemCreatedIntegrationEvent(
    Guid ProductId,
    string ProductName): IntegrationEventBase;