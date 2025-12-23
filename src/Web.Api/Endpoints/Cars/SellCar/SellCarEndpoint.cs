using Application.Cars.SellCar;
using Ardalis.Result.AspNetCore;
using SharedKernel.Messaging;
using Web.Api.Extensions;

namespace Web.Api.Endpoints.Cars.SellCars;

public partial class SellCarEndpoint : Endpoint<SellCarRequest>
{
    protected override string EndpointPath => "api/cars/{carId:guid}/sell";

    protected override Delegate Handler => async (Guid carId,
            SellCarRequest request,
            ICommandHandler<SellCarCommand> handler, CancellationToken cancellationToken) =>
        {
            var command = new SellCarCommand(carId, request.SalePrice);

            var result = await handler.Handle(command, cancellationToken);

            return result.ToMinimalApiResult();
        };

    protected override HttpVerb HttpVerb => HttpVerb.PUT;
     
    protected override void Configure()
    {
        Name("SellCar");
        AddTag("Cars");
        RequirePermission("Cars.Sell");
    }
}
