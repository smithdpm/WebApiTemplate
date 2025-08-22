
using Application.Abstractions.Messaging;
using Application.Cars;
using Application.Cars.GetById;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;

namespace Web.Api.Endpoints.Cars;

public class GetCarByIdEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/cars/{id:guid}", async (Guid id,
            IQueryHandler<GetCarByIdQuery, CarDto> handler,
            CancellationToken cancellationToken) =>
            {
                var query = new GetCarByIdQuery(id);
                Result<CarDto> result = await handler.Handle(query, cancellationToken);
                return result.ToMinimalApiResult();
            })
            .WithName("GetCarById")
            .WithOpenApi();
    }
}
