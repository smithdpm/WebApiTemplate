using Domain.Cars;
using Domain.Cars.Events;
using Infrastructure.IdentityGeneration;
using Shouldly;

namespace UnitTests.Domain.Cars;

public class CarTest
{
    private readonly CarFactory _carFactory;
    public CarTest()
    {
        _carFactory = new CarFactory(new UuidSqlServerFriendlyGenerator());
    }

    [Fact]
    public void SellCar_ShouldReturnSucess_WhenValid()
    {
        // Arrange
        var car = _carFactory.Create("Toyota", "Corolla", 2020, 15000, 20000m);
        var soldPrice = 21000m;

        // Act
        var result = car.SellCar(soldPrice);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void SellCar_ShouldUpdateValues_WhenValid()
    {
        // Arrange
        var car = _carFactory.Create("Toyota", "Corolla", 2020, 15000, 20000m);
        var soldPrice = 21000m;

        // Act
        var result = car.SellCar(soldPrice);

        // Assert
        car.SoldPrice.ShouldBe(soldPrice);
        car.IsSold.ShouldBeTrue();
    }

    [Fact]
    public void SellCar_ShouldRaiseEvent_WhenValid()
    {
        // Arrange
        var car = _carFactory.Create("Toyota", "Corolla", 2020, 15000, 20000m);
        var soldPrice = 21000m;

        // Act
        var result = car.SellCar(soldPrice);

        // Assert
        car.DomainEvents.Count.ShouldBe(1);
        car.DomainEvents.First().ShouldBeOfType<CarSoldEvent>();
    }

    [Fact]
    public void SellCar_ShouldReturnError_IfCarIsAlreadySold()
    {
        // Arrange
        var car = _carFactory.Create("Toyota", "Corolla", 2020, 15000, 20000m);
        var soldPrice = 21000m;

        // Act
        var result = car.SellCar(soldPrice);
        var secondResult = car.SellCar(soldPrice);

        // Assert
        secondResult.IsSuccess.ShouldBeFalse();
        secondResult.Status.ShouldBe(Ardalis.Result.ResultStatus.Error);
    }

    [Fact]
    public void SellCar_ShouldNotUpdateValues_IfCarIsAlreadySold()
    {
        // Arrange
        var car = _carFactory.Create("Toyota", "Corolla", 2020, 15000, 20000m);
        var soldPrice = 21000m;
        var secondSoldPrice = 21000m;

        // Act
        var result = car.SellCar(soldPrice);
        var secondResult = car.SellCar(secondSoldPrice);

        // Assert
        car.SoldPrice.ShouldBe(soldPrice);
        car.IsSold.ShouldBeTrue();
    }
}
