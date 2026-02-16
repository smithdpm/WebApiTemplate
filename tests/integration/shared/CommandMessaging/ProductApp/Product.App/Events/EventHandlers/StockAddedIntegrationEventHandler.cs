using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;
using Product.App.Model;
using Product.App.Model.Specification;
using SharedKernel.Database;

namespace Product.App.Events.EventHandlers;

public class StockAddedIntegrationEventHandler(IRepository<ProductItem> repository) : IntegrationEventHandler<StockAddedIntegrationEvent>
{
    public override async Task<Result> HandleAsync(StockAddedIntegrationEvent input, CancellationToken cancellationToken)
    {
        var spec = new ProductItemByProductNameSpec(input.ProductName);
        var product = await repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (product == null)
            return Result.Invalid();

        product.UpdateTotalShipped(input.QuantityAdded);
        return Result.Success();
    }
}
