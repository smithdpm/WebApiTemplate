using Cqrs.Events.IntegrationEvents;

namespace Product.App.Events;

public record ProductsPurchasedIntegrationEvent(
    Guid PurchaseId,
    List<SoldProduct> SoldProducts) : IntegrationEventBase;

public record SoldProduct(string ProductName, int Quantity);
