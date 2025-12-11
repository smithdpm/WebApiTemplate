
using Domain.Abstractions;
using Domain.Cars;
using NSubstitute;

namespace UnitTests.Domain.Cars;

public class CarFactoryTests
{
    private readonly CarFactory _carFactory;
    private readonly IIdGenerator<Guid> _idGenerator = Substitute.For<IIdGenerator<Guid>>();
    public CarFactoryTests()
    {
        _carFactory = new CarFactory(_idGenerator);
    }

    [Fact()]
    public void Create_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var make = "Toyota";
        var model = "Corolla";
        var year = 2020;
        var mileage = 15000;
        var price = 20000m;
        
        _idGenerator.NewId().Returns(id);

        // Act
        var car = _carFactory.Create(make, model, year, mileage, price);

        // Assert
        Assert.Equal(id, car.Id);
        Assert.Equal(make, car.Make);
        Assert.Equal(model, car.Model);
        Assert.Equal(year, car.Year);
        Assert.Equal(mileage, car.Mileage);
        Assert.Equal(price, car.Price);
    }
}