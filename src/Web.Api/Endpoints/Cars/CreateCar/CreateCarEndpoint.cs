using Application.Cars.Create;
using Mapster;
using SharedKernel.Messaging;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Web.Api.Extensions;

namespace Web.Api.Endpoints.Cars.CreateCar3;

public class CreateCar3Endpoint : Endpoint<CreateCarRequest>
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
        Result<Guid> result = await handler.Handle(request.Adapt<CreateCarCommand>(), cancellationToken);

        return result.ToMinimalApiResult();
    };

}
