using SharedKernel.Messaging;
using Application.Cars.Create;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using System.Text.RegularExpressions;
using Web.Api.Extensions;

namespace Web.Api.Endpoints.Cars;

public class CreateCarEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/cars", async (CreateCarCommand request,
            ICommandHandler<CreateCarCommand, Guid> handler,
            CancellationToken cancellationToken) =>
                {
                    Result<Guid> result = await handler.Handle(request, cancellationToken);

                    return result.ToMinimalApiResult();
                })
        .WithName("CreateCar")
        .WithOpenApi()
        .RequirePermission("Cars.Create");
    }
}
