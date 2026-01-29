
using SharedKernel.Events;

namespace Shop.Domain.Purchases;

public record PurchaseCreatedDomainEvent(
    Guid PurchaseId,
    List<SoldProduct> SoldProducts): DomainEventBase;