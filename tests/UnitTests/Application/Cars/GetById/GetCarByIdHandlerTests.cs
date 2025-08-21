using Application.Abstractions.Database;
using Application.Cars;
using Application.Cars.GetById;
using Ardalis.Specification;
using Domain.Cars;
using Domain.Cars.Specifications;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Shouldly;
using Xunit;

namespace UnitTests.Application.Cars.GetById;

public class GetCarByIdHandlerTests
{
    private readonly GetCarByIdHandler _handler;
    private readonly IReadRepository<Car> _repository = Substitute.For<IReadRepository<Car>>();
    public GetCarByIdHandlerTests()
    {
        _handler = new GetCarByIdHandler(_repository);
    }

    [Fact()]
    public async Task Handle_ShouldUseCarByIdSpec_WhenQueryIsValid()
    {
        // Arrange
        var testCar = new Car("Toyota", "Corolla", 2020, 15000, 20000m);

        var query = new GetCarByIdQuery(testCar.Id);

        ISpecification<Car> capturedSpec = null;

        await _repository.ProjectToFirstOrDefaultAsync<CarDto>(Arg.Do<ISpecification<Car>>(arg => capturedSpec = arg), Arg.Any<CancellationToken>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedSpec.ShouldNotBeNull();
        capturedSpec.ShouldBeOfType<CarByIdSpec>();
        capturedSpec.IsSatisfiedBy(testCar).ShouldBeTrue();
    }

    [Fact()]
    public async Task Handle_ShouldReturnSucess_WhenCarExists()
    {
        // Arrange
        var carId = Guid.Parse("381da394-fae2-418e-988a-0f6d181360a6");
        var testCar = new CarDto(carId, "Toyota", "Corolla", 2020, 15000, 20000m);

        var query = new GetCarByIdQuery(testCar.Id);

        _repository.ProjectToFirstOrDefaultAsync<CarDto>(Arg.Any<ISpecification<Car>>(),
            Arg.Any<CancellationToken>())
            .Returns(testCar);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(testCar);
    }


    [Fact()]
    public async Task Handle_ShouldReturnFailure_WhenCarDoesNotExist()
    {
        // Arrange
        var carId = Guid.Parse("381da394-fae2-418e-988a-0f6d181360a6");

        var query = new GetCarByIdQuery(carId);

        _repository.ProjectToFirstOrDefaultAsync<CarDto>(Arg.Any<ISpecification<Car>>(),
            Arg.Any<CancellationToken>())
            .ReturnsNull();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}