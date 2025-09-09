using Ardalis.Specification;
using Domain.Cars;

namespace Application.Cars.GetById;

public class GetCarDtoByIdSpec : Specification<Car, CarDto>, ISingleResultSpecification<Car, CarDto>
{
    public GetCarDtoByIdSpec(Guid carId)
    {
        Query.Where(car => car.Id == carId)
            .EnableCache(nameof(GetCarDtoByIdSpec), carId)
            .Select(car => new CarDto(
            car.Id,
            car.Make,
            car.Model,
            car.Year,
            car.Mileage,
            car.Price
           ));
    }
}
