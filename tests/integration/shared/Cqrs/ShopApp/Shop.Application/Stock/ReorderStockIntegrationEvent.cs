using Cqrs.Events.IntegrationEvents;

namespace Shop.Application.Stock;

public record ReorderStockIntegrationEvent(
    string ProductName,
    int ReorderQuantity
) : IntegrationEventBase;
