using Domain.Cars;

namespace UnitTests.Domain.Cars;

public class CarTest
{
    [Fact]
    public void InitialiseCar_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var make = "Toyota";
        var model = "Corolla";
        var year = 2020;
        var mileage = 15000;
        var price = 20000m;
        
        // Act
        var car = new Car(make, model, year, mileage, price);
        
        // Assert
        Assert.Equal(make, car.Make);
        Assert.Equal(model, car.Model);
        Assert.Equal(year, car.Year);
        Assert.Equal(mileage, car.Mileage);
        Assert.Equal(price, car.Price);
    }

}
