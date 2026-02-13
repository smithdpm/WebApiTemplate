using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;
using Shop.Application.Database;
using Shop.Application.IntegrationEvents.Events;
using Shop.Domain.Aggregates.Stock;

namespace Shop.Application.IntegrationEvents.EventHandlers;


public class ProductItemCreatedEventHandler(ApplicationDbContext dbContext) : IntegrationEventHandler<ProductItemCreatedIntegrationEvent>
{
    public override async Task<Result> HandleAsync(ProductItemCreatedIntegrationEvent input, CancellationToken cancellationToken)
    {
        var productExists = dbContext.ProductStocks
            .Where(s => s.ProductName == input.ProductName)
            .Any();

        if (productExists)
            return Result.Success();

        // Add new product with some tester stock :)
        var newProduct = new ProductStock(input.ProductName, 10);
        dbContext.ProductStocks.Add(newProduct);

        return Result.Success();
    }
}
