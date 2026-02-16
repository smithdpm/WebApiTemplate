using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;
using Product.App.Model;
using SharedKernel.Database;

namespace Product.App.Events.EventHandlers;

public class ProductsPurchasedIntergrationEventHandler(IRepository<ProductItem> repository) : IntegrationEventHandler<ProductsPurchasedIntegrationEvent>
{
    public override async Task<Result> HandleAsync(ProductsPurchasedIntegrationEvent input, CancellationToken cancellationToken)
    {
        var products = await repository.ListAsync(cancellationToken);

        foreach (var soldProduct in input.SoldProducts)
        {
            var product = products.FirstOrDefault(p => p.Name == soldProduct.ProductName);
            if (product is not null)
            {
                product.TotalSold += soldProduct.Quantity;
                await repository.UpdateAsync(product, cancellationToken);
            }
        }

        return Result.Success();
    }
}
