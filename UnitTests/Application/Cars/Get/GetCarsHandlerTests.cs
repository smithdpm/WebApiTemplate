using Application.Abstractions.Database;
using Application.Cars;
using Application.Cars.Get;
using Domain.Cars;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Application.Cars.Get;

public class GetCarsHandlerTests
{
    private readonly GetCarsHandler _handler;
    private readonly IRepository<Car> _repository = Substitute.For<IRepository<Car>>();

    public GetCarsHandlerTests()
    {
        _handler = new GetCarsHandler(_repository);
    }

    [Fact()]
    public async Task HandleTest()
    {
        // Arrange
        var query = new GetCarsQuery(null, null);
        var cars = GetCars();

        _repository.ListAsync(Arg.Any<CancellationToken>()).Returns(cars);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEquivalentTo(GetCarsDto(cars));
    }

    private List<Car> GetCars()
    {
        return new List<Car>
        {
            new Car("Toyota", "Corolla", 2020, 15000, 20000m),
            new Car("Honda", "Civic", 2019, 20000, 18000m),
            new Car("Ford", "Focus", 2021, 10000, 22000m)
        };
    }

    private List<CarDto> GetCarsDto(List<Car> cars)
    {
        return cars.Select(car =>
            new CarDto(car.Id, car.Make, car.Model, car.Year, car.Mileage, car.Price)
        ).ToList();
    }

}