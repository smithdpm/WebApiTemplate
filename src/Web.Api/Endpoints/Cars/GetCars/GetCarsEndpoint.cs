
using Application.Cars;
using Application.Cars.Get;
using Application.Cars.GetByMake;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Cqrs.Operations.Queries;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using ReprEndpoints.Endpoints;

namespace Web.Api.Endpoints.Cars.GetCars;

public class GetCarsEndpoint : NoRequestEndpoint<GetCarsResponse>
{
    protected override string EndpointPath => "api/cars";
    protected override HttpVerb HttpVerb => HttpVerb.GET;

    protected override void Configure()
    {
        Name("GetCarsById");
        AddTag("Cars");
    }
    protected override Delegate Handler =>
    async ([FromQuery] string? make,
        IQueryHandler<GetCarsQuery, List<CarDto>> getAllHandler,
        IQueryHandler<GetCarsByMakeQuery, List<CarDto>> getByMakeHandler,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(make))
        {
            var query = new GetCarsQuery(null, null);
            Result<List<CarDto>> results = await getAllHandler.HandleAsync(query, cancellationToken);

            return results.Map(cars => new GetCarsResponse(cars.Adapt<List<Car>>()))
                .ToMinimalApiResult();
        }
        else
        {
            var query = new GetCarsByMakeQuery(make);
            var results = await getByMakeHandler.HandleAsync(query, cancellationToken);

            return results.Map(cars => new GetCarsResponse(cars.Adapt<List<Car>>()))
                .ToMinimalApiResult();
        }
    };
}
