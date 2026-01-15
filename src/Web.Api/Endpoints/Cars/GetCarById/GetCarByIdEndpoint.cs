using Application.Cars;
using Application.Cars.GetById;
using Ardalis.Result;
using Ardalis.Result.AspNetCore;
using Cqrs.Messaging;
using Mapster;

namespace Web.Api.Endpoints.Cars.GetCarById;

public class GetCarByIdEndpoint : NoRequestEndpoint<GetCarByIdResponse>
{
    protected override string EndpointPath => "api/cars/{id:guid}";
    protected override HttpVerb HttpVerb => HttpVerb.GET;
    protected override Delegate Handler => async (Guid id,
            IQueryHandler<GetCarByIdQuery, CarDto> handler,
            CancellationToken cancellationToken) =>
            {
                var query = new GetCarByIdQuery(id);
                Result<CarDto> result = await handler.Handle(query, cancellationToken);
                return result
                    .Map(carDto => carDto.Adapt<GetCarByIdResponse>())
                    .ToMinimalApiResult();
            };
    
    protected override void Configure()
    {
        Name("GetCarById");
        AddTag("Cars");
    }
}
