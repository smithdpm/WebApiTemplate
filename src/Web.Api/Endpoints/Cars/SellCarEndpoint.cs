using Application.Cars.SellCar;
using Ardalis.Result.AspNetCore;
using SharedKernel.Messaging;
using Web.Api.Extensions;

namespace Web.Api.Endpoints.Cars;

public class SellCarEndpoint: IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/cars/{carId:guid}/sell", async (Guid carId, 
            SellCarRequest request, 
            ICommandHandler<SellCarCommand> handler, CancellationToken cancellationToken) =>
        {
            var command = new SellCarCommand(carId, request.SalePrice);
            
            var result = await handler.Handle(command, cancellationToken);          
            
            return result.ToMinimalApiResult();
        })
        .WithName("SellCar")
        .WithTags("Cars")
        .WithOpenApi()
        .RequirePermission("Cars.Sell");
    }

    public record SellCarRequest(decimal SalePrice);
}
