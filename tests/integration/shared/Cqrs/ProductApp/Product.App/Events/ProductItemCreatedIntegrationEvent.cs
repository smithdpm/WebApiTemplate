using Cqrs.Events.IntegrationEvents;
using SharedKernel.Events;

namespace Product.App.Events;

public record ProductItemCreatedIntegrationEvent(
    Guid ProductId,
    string ProductName): IntegrationEventBase;