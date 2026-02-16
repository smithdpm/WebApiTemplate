using SharedKernel.Database;
using Domain.Cars;
using Ardalis.Result;
using Cqrs.Operations.Queries;

namespace Application.Cars.Get;

public class GetCarsHandler(IRepository<Car> repository) : QueryHandler<GetCarsQuery, List<CarDto>>
{
    public override async Task<Result<List<CarDto>>> HandleAsync(GetCarsQuery query, CancellationToken cancellationToken)
    {
        var cars = await repository.ListAsync(cancellationToken);

        var carDtos = cars.Select(car => new CarDto(
            Id: car.Id,
            Make: car.Make,
            Model: car.Model,
            Year: car.Year,
            Mileage: car.Mileage,
            Price: car.Price
           )).ToList();

        return carDtos;
    }
}
