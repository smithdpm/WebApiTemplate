using Ardalis.Specification;
using Domain.Cars;


namespace Application.Cars;

public class CarToCarDtoSpec : Specification<Car,CarDto>
{
    public CarToCarDtoSpec()
    {
        Query.Select(car => new CarDto(
            car.Id,
            car.Make,
            car.Model,
            car.Year,
            car.Mileage,
            car.Price
           ));
    }
}