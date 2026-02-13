using Cqrs.Events.IntegrationEvents;

namespace Shop.Application.IntegrationEvents.Events;

public record ProductItemCreatedIntegrationEvent(
    Guid ProductId,
    string ProductName): IntegrationEventBase;