using SharedKernel.Database;
using SharedKernel.Messaging;
using Domain.Cars;
using Domain.Cars.Specifications;
using Ardalis.Result;

namespace Application.Cars.GetById;

public class GetCarByIdHandler(IReadRepository<Car> repository) : IQueryHandler<GetCarByIdQuery, CarDto>
{
    public async Task<Result<CarDto>> Handle(GetCarByIdQuery query, CancellationToken cancellationToken)
    {
        var spec = new CarByIdSpec(query.CarId);
        var car = await repository.ProjectToFirstOrDefaultAsync<CarDto>(spec, cancellationToken);

        if (car is null)
        {
            return Result<CarDto>.NotFound($"Car with id: {query.CarId} not found.");
        }

        return car;
    }
}
