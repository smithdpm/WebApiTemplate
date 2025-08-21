using Application.Abstractions.Database;
using Application.Cars;
using Application.Cars.GetById;
using Ardalis.Specification;
using Domain.Cars;
using Domain.Cars.Specifications;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Domain.Cars.Specifications;

public class CarByIdSpecTests
{
    [Fact()]
    public void CarByIdSpec_ShouldReturnCar_WhenIdMatches()
    {
        // Arrange
        var carWeAreLookingFor = new Car("Toyota", "Corolla", 2020, 15000, 20000m);
        var carWeDontWant = new Car("Renault", "Captur", 2022, 15000, 20000m);
        var cars = new List<Car> { carWeAreLookingFor, carWeDontWant };

        var spec = new CarByIdSpec(carWeAreLookingFor.Id);

        // Act
        var filteredCars = spec.Evaluate(cars);

        // Assert
        filteredCars.ShouldHaveSingleItem();
        filteredCars.First().ShouldBe(carWeAreLookingFor);
    }
}