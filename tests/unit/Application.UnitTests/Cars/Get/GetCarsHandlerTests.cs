using Application.Cars;
using Application.Cars.Get;
using Domain.Cars;
using Infrastructure.IdentityGeneration;
using NSubstitute;
using SharedKernel.Database;
using Shouldly;
using Xunit;

namespace Application.UnitTests.Cars.Get;

public class GetCarsHandlerTests
{
    private readonly GetCarsHandler _handler;
    private readonly IRepository<Car> _repository = Substitute.For<IRepository<Car>>();
    private readonly CarFactory _carFactory;
    public GetCarsHandlerTests()
    {
        _carFactory = new CarFactory(new UuidSqlServerFriendlyGenerator());
        _handler = new GetCarsHandler(_repository);
    }

    [Fact()]
    public async Task Handle_ShouldReturnSucessAndCars_WhenQueryIsValid()
    {
        // Arrange
        var query = new GetCarsQuery(null, null);
        var cars = GetCars();
        var cancellationToken = TestContext.Current.CancellationToken;

        _repository.ListAsync(Arg.Any<CancellationToken>()).Returns(cars);

        // Act
        var result = await _handler.HandleAsync(query, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEquivalentTo(GetCarsDto(cars));
    }

    private List<Car> GetCars()
    {
        return new List<Car>
        {
            _carFactory.Create("Toyota", "Corolla", 2020, 15000, 20000m),
            _carFactory.Create("Honda", "Civic", 2019, 20000, 18000m),
            _carFactory.Create("Ford", "Focus", 2021, 10000, 22000m)
        };
    }

    private List<CarDto> GetCarsDto(List<Car> cars)
    {
        return cars.Select(car =>
            new CarDto(car.Id, car.Make, car.Model, car.Year, car.Mileage, car.Price)
        ).ToList();
    }

}