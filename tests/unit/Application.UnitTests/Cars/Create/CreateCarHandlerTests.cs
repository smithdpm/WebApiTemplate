using Application.Cars.Create;
using Domain.Cars;
using NSubstitute;
using SharedKernel.Database;
using Shouldly;
using Infrastructure.IdentityGeneration;

namespace Application.UnitTests.Cars.Create;

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
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _handler.HandleAsync(command, cancellationToken);

        // Assert   
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
    }
      
}