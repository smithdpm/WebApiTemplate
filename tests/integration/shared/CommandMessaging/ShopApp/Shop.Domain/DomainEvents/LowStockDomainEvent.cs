using SharedKernel.Events;

namespace Shop.Domain.DomainEvents;

public record LowStockDomainEvent
    (Guid ProductStockId, 
    string ProductName) 
    : DomainEventBase;