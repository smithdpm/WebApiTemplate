using Cqrs.Events.IntegrationEvents;
using Shop.Domain.Aggregates.Purchases;

namespace Shop.Application.IntegrationEvents.Events;

public record ProductsPurchasedIntegrationEvent(
    Guid PurchaseId,
    List<SoldProduct> SoldProducts) : IntegrationEventBase;
