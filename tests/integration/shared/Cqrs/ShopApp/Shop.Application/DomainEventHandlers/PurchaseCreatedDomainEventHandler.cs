using Ardalis.Result;
using Cqrs.Events.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Database;
using Shop.Domain.DomainEvents;

namespace Shop.Application.DomainEventHandlers;

public class PurchaseCreatedDomainEventHandler(ApplicationDbContext applicationDbContext) : DomainEventHandler<PurchaseCreatedDomainEvent>
{
    public override async Task<Result> HandleAsync(PurchaseCreatedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var productStocks = await applicationDbContext.ProductStocks
            .Where(ps => domainEvent.SoldProducts.Select(sp => sp.ProductName).Contains(ps.ProductName))
            .ToListAsync(cancellationToken);

        foreach (var stockedProduct in productStocks)
        {
            var  quantitySold = domainEvent.SoldProducts
                .Where(sp => sp.ProductName == stockedProduct.ProductName)
                .Sum(sp => sp.Quantity);

            stockedProduct.RemoveStock(quantitySold);
        }
        return Result.Success();
    }
}
