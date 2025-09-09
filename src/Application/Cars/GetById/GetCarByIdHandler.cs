using SharedKernel.Database;
using SharedKernel.Messaging;
using Domain.Cars;
using Domain.Cars.Specifications;
using Ardalis.Result;
using Ardalis.Specification;

namespace Application.Cars.GetById;

public class GetCarByIdHandler(IReadRepository<Car> repository) : IQueryHandler<GetCarByIdQuery, CarDto>
{
    public async Task<Result<CarDto>> Handle(GetCarByIdQuery query, CancellationToken cancellationToken)
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
