namespace Domain.Cars;

public interface ICarFactory: IEntityFactory<Car, Guid>
{     
    Car Create(string make, string model, int year, int mileage, decimal price);
}
