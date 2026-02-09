
using SharedKernel.Events;
using Shop.Domain.Aggregates.Purchases;

namespace Shop.Domain.DomainEvents;

public record PurchaseCreatedDomainEvent(
    Guid PurchaseId,
    List<SoldProduct> SoldProducts): DomainEventBase;