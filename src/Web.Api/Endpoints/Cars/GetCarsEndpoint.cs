using Application.Cars;
using Application.Cars.Get;
using Application.Cars.GetByMake;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Messaging;

namespace Web.Api.Endpoints.Cars;

public class GetCarsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/cars", async ([FromQuery] string? make,
            IQueryHandler<GetCarsQuery, List<CarDto>> getAllHandler,
            IQueryHandler<GetCarsByMakeQuery, List<CarDto>> getByMakeandler,
            CancellationToken cancellationToken) =>
                {
                    if (string.IsNullOrWhiteSpace(make))
                    {
                        var query = new GetCarsQuery(null, null);
                        Result<List<CarDto>> results = await getAllHandler.Handle(query, cancellationToken);
                        return results.ToMinimalApiResult();
                    }
                    else
                    {
                        var query = new GetCarsByMakeQuery(make);
                        var results = await getByMakeandler.Handle(query, cancellationToken);
                        return results.ToMinimalApiResult();
                    }          
                })
        .WithName("GetCars")
        .WithTags("Cars")
        .WithOpenApi();
    }

}
