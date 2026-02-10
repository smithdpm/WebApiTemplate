using Ardalis.Result;
using Cqrs.Events.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Database;
using Shop.Application.IntegrationEvents.Events;
using Shop.Domain.Aggregates.Purchases;
using Shop.Domain.DomainEvents;

namespace Shop.Application.DomainEventHandlers;

public class PurchaseCreatedDomainEventHandler(ApplicationDbContext applicationDbContext) : DomainEventHandler<PurchaseCreatedDomainEvent>
{
    public override async Task<Result> HandleAsync(PurchaseCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var productStocks = await applicationDbContext.ProductStocks
            .Where(ps => domainEvent.Purchase.SoldProducts.Select(sp => sp.ProductName).Contains(ps.ProductName))
            .ToListAsync(cancellationToken);

        var productsSold = new List<SoldProduct>();
        foreach (var stockedProduct in productStocks)
        {
            var  productSold = domainEvent.Purchase.SoldProducts
                .Where(sp => sp.ProductName == stockedProduct.ProductName)
                .First();

            productsSold.Add(productSold);
            stockedProduct.RemoveStock(productSold.Quantity);
        }
        AddIntegrationEvent(new ProductsPurchasedIntegrationEvent(domainEvent.Purchase.Id, productsSold));
        return Result.Success();
    }
}
