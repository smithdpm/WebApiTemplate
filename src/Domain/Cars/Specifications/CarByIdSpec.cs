using Ardalis.Specification;

namespace Domain.Cars.Specifications;

public class CarByIdSpec: Specification<Car>, ISingleResultSpecification<Car>
{
    public CarByIdSpec(Guid carId) => Query.Where(car => car.Id == carId);
}
