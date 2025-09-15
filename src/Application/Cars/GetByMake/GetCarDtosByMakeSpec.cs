
using Ardalis.Specification;
using Domain.Cars;
using SharedKernel;

namespace Application.Cars.GetByMake;
public class GetCarDtosByMakeSpec: Specification<Car, CarDto>
{
    public GetCarDtosByMakeSpec(string make)
    {
        Query.Where(car => car.Make == make)
            .EnableCache(nameof(GetCarDtosByMakeSpec), make)
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
