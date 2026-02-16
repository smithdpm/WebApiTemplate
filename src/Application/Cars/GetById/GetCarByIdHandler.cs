using SharedKernel.Database;
using Domain.Cars;
using Domain.Cars.Specifications;
using Ardalis.Result;
using Ardalis.Specification;
using Cqrs.Operations.Queries;

namespace Application.Cars.GetById;

public class GetCarByIdHandler(IReadRepository<Car> repository) : QueryHandler<GetCarByIdQuery, CarDto>
{
    public override async Task<Result<CarDto>> HandleAsync(GetCarByIdQuery query, CancellationToken cancellationToken)
    {
        var spec = new GetCarDtoByIdSpec(query.CarId);
        
        var car = await repository.SingleOrDefaultAsync(spec, cancellationToken);

        if (car is null)
        {
            return Result<CarDto>.NotFound($"Car with id: {query.CarId} not found.");
        }

        return car;
    }
}
