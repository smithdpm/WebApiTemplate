
using Domain.Abstractions;

namespace Domain.Cars;
public class CarFactory(IIdGenerator<Guid> idGenerator): ICarFactory
{
    public Car Create(string make, string model, int year, int mileage, decimal price)
    {
        return new Car(
            id: idGenerator.NewId(),
            make: make,
            model: model,
            year: year,
            mileage: mileage,
            price: price
            );
    }
}
