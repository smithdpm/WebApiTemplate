using Domain.Cars;
using Domain.Cars.Specifications;
using Domain.UnitTests.TestHelpers;
using Shouldly;

namespace Domain.UnitTests.Cars.Specifications;

public class CarByIdSpecTests
{
    private readonly CarFactory _carFactory;
    public CarByIdSpecTests()
    {
        _carFactory = new CarFactory(new IdGenerator());
    }

    [Fact()]
    public void CarByIdSpec_ShouldReturnCar_WhenIdMatches()
    {
        // Arrange
        var carWeAreLookingFor = _carFactory.Create("Toyota", "Corolla", 2020, 15000, 20000m);
        var carWeDontWant = _carFactory.Create("Renault", "Captur", 2022, 15000, 20000m);
        var cars = new List<Car> { carWeAreLookingFor, carWeDontWant };

        var spec = new CarByIdSpec(carWeAreLookingFor.Id);

        // Act
        var filteredCars = spec.Evaluate(cars);

        // Assert
        filteredCars.ShouldHaveSingleItem();
        filteredCars.First().ShouldBe(carWeAreLookingFor);
    }
}