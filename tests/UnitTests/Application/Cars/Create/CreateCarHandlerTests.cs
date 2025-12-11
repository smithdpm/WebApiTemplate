using Application.Cars.Create;
using Domain.Abstractions;
using Domain.Cars;
using NSubstitute;
using SharedKernel.Database;
using Shouldly;
using Xunit;
using Infrastructure.IdentityGeneration;

namespace UnitTests.Application.Cars.Create;

public class CreateCarHandlerTests
{
    private readonly CreateCarHandler _handler;
    private readonly IRepository<Car> _repository = Substitute.For<IRepository<Car>>();
    
    public CreateCarHandlerTests()
    {
        var idGenerator = new UuidSqlServerFriendlyGenerator(); 
        var carFactory = new CarFactory(idGenerator);
        _handler = new CreateCarHandler(_repository, carFactory);
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