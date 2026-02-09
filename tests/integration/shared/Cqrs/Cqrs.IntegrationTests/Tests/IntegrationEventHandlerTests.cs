using Ardalis.Result;
using Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;
using Cqrs.IntegrationTests.Fixtures.ClassFixtures;
using Cqrs.Messaging;
using Microsoft.AspNetCore.TestHost;
using Polly;
using Product.App.UseCases;
using Shop.Application.Stock;
using Shop.Domain.Aggregates.Stock;
using Shouldly;

namespace Cqrs.IntegrationTests.Tests;

public class IntegrationEventHandlerTests: IClassFixture<MultiAppFixture>
{
    private readonly TestServer _shopServer;
    private readonly TestServer _productServer;
    private readonly MultiAppFixture _multiAppFixture;
    private readonly DatabaseServerFixture _databaseServerFixture;
    private readonly ServiceBusFixture _serviceBusFixture;

    public IntegrationEventHandlerTests(DatabaseServerFixture databaseServerFixture,
        ServiceBusFixture serviceBusFixture,
        MultiAppFixture multiAppFixture)
    {
        _databaseServerFixture = databaseServerFixture;
        _serviceBusFixture = serviceBusFixture;
        _multiAppFixture = multiAppFixture;
        _shopServer = _multiAppFixture.ShopAppFactory.Server;
        _productServer = _multiAppFixture.ProductAppFactory.Server;
    }

    [Fact]
    public async Task IntegrationEventRaisedByServiceACommandHandler_ShouldBeSuccuessfullyHandledByServiceB()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        using var productScope = _productServer.Services.CreateScope();
        
        var handler = productScope.ServiceProvider.GetRequiredService<ICommandHandler<CreateProdcutItemCommand, Guid>>();

        var newProductCommand = new CreateProdcutItemCommand("Grapefruit", "Yellow & Sour.", "Fruit", 0.55m);

        //Act
        await handler.HandleAsync(newProductCommand, cancellationToken);

        using var shopScope = _shopServer.Services.CreateScope();
        var queryHandler = shopScope.ServiceProvider
            .GetRequiredService<IQueryHandler<GetStockByProductNameQuery, ProductStock>>();

        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        var grapefruitStock = await WaitForNonNullResult(() =>
        {
            return queryHandler.Handle(new GetStockByProductNameQuery(newProductCommand.Name), cancellationToken);
        });

        // Assert
        grapefruitStock.IsSuccess.ShouldBeTrue();
        grapefruitStock.Value.ShouldNotBeNull();
        grapefruitStock.Value.ProductName.ShouldBe(newProductCommand.Name);

    }

    private static Task<Result<T>> WaitForNonNullResult<T>(Func<Task<Result<T>>> action)
    {
        var retryPolicy = Policy<Result<T>>
            .HandleResult(response => response.IsNoContent())
            .WaitAndRetryAsync(50, _ => TimeSpan.FromMilliseconds(200));

        var result = retryPolicy.ExecuteAsync(action);

        return result!;
    }
}
