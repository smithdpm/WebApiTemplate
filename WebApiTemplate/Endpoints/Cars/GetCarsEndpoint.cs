using Application.Abstractions.Messaging;
using Application.Cars;
using Application.Cars.Get;
using SharedKernel;

namespace Web.Api.Endpoints.Cars;

public class GetCarsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/cars", async (IQueryHandler<GetCarsQuery, List<CarDto>> handler,
            CancellationToken cancellationToken) =>
                {
                    var query = new GetCarsQuery(null, null);
                    Result<List<CarDto>> result = await handler.Handle(query, cancellationToken);

                    return result;
                })
        .WithName("GetCars")
        .WithOpenApi();
    }
}
