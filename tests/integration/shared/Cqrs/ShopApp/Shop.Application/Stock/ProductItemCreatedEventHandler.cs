using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;

namespace Shop.Application.Stock;


public class ProductItemCreatedEventHandler : IntegrationEventHandler<ProductItemCreatedIntegrationEvent>
{
    public override Task<Result> HandleAsync(ProductItemCreatedIntegrationEvent input, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
