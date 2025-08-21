using Application.Abstractions.Database;
using Application.Cars.Create;
using Domain.Cars;
using NSubstitute;
using Shouldly;
using Xunit;

namespace UnitTests.Application.Cars.Create;

public class CreateCarHandlerTests
{
    private readonly CreateCarHandler _handler;
    private readonly IRepository<Car> _repository = Substitute.For<IRepository<Car>>();
    public CreateCarHandlerTests()
    {
        _handler = new CreateCarHandler(_repository);
    }

    [Fact()]
    public async Task Handle_ShouldReturnSucess_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateCarCommand("Toyota", "Corolla", 2020, 15000, 20000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert   
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
    }
      
}