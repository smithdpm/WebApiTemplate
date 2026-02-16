using Application.Cars.Create;
using Mapster;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using ReprEndpoints.Endpoints;
using Cqrs.Operations.Commands;

namespace Web.Api.Endpoints.Cars.CreateCar;

public class CreateCarEndpoint : Endpoint<CreateCarRequest>
{
    protected override string EndpointPath => "api/cars";

    protected override HttpVerb HttpVerb => HttpVerb.POST;
    protected override void Configure()
    {
        Name("CreateCar");
        AddTag("Cars");
        RequirePermission("Cars.Create");
    }
    protected override Delegate Handler => 
        async (CreateCarRequest request, ICommandHandler<CreateCarCommand, Guid> handler, CancellationToken cancellationToken) =>
    {
        Result<Guid> result = await handler.HandleAsync(request.Adapt<CreateCarCommand>(), cancellationToken);

        return result.ToMinimalApiResult();
    };

}
