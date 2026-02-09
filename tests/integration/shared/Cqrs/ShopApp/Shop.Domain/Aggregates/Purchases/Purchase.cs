using SharedKernel.Events;
using Shop.Domain.DomainEvents;

namespace Shop.Domain.Aggregates.Purchases;

public class Purchase : HasDomainEvents
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public List<SoldProduct>  SoldProducts { get; set; } = new();

    public Purchase() { }
    public Purchase(List<SoldProduct> soldProducts)
    {
        SoldProducts = soldProducts;
        AddDomainEvent(new PurchaseCreatedDomainEvent(Id, soldProducts));
    }
}
