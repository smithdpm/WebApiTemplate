using Ardalis.Result;
using Cqrs.IntegrationTests.Extensions;
using Cqrs.IntegrationTests.Fixtures.ClassFixtures;
using Cqrs.Messaging;
using Microsoft.AspNetCore.TestHost;
using Polly;
using Product.App.Model;
using Product.App.UseCases;
using Shop.Application.Purchases;
using Shop.Application.Stock;
using Shop.Domain.Aggregates.Stock;
using Shouldly;

namespace Cqrs.IntegrationTests.Tests;

[Collection("IntegrationEventHandlerTests")]
public class IntegrationEventHandlerTests : IClassFixture<MultiAppFixture>, IAsyncLifetime
{
    private readonly TestServer _shopServer;
    private readonly TestServer _productServer;
    private readonly MultiAppFixture _multiAppFixture;

    public IntegrationEventHandlerTests(MultiAppFixture multiAppFixture)
    {
        _multiAppFixture = multiAppFixture;     
        _shopServer = _multiAppFixture.ShopAppFactory.Server;
        _productServer = _multiAppFixture.ProductAppFactory.Server;
    }
    public async ValueTask InitializeAsync()
    {
        await _multiAppFixture.ReseedDatabases();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task IntegrationEventRaisedByServiceACommandHandler_ShouldBeSuccuessfullyHandledByServiceB()
    {
        // (ProductApp) CreateProdcutItemCommandHandler --> ProductItemCreatedIntegrationEvent
        // ==> (ShopApp) ProductItemCreatedIntegrationEventHandler

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var newProductCommand = new CreateProdcutItemCommand("Grapefruit", "Yellow & Sour.", "Fruit", 0.55m);
        
        //Act
        await _productServer.HandleCommand<CreateProdcutItemCommand, Guid>(newProductCommand, cancellationToken);

        using var shopScope = _shopServer.Services.CreateScope();
        var queryHandler = shopScope.ServiceProvider
            .GetRequiredService<IQueryHandler<GetStockByProductNameQuery, ProductStock>>();

        var grapefruitStock = await WaitForNonNullResult(() =>
        {
            return _shopServer.HandleQuery<GetStockByProductNameQuery, ProductStock>(
                new GetStockByProductNameQuery(newProductCommand.Name), cancellationToken);
        });

        // Assert
        grapefruitStock.IsSuccess.ShouldBeTrue();
        grapefruitStock.Value.ShouldNotBeNull();
        grapefruitStock.Value.ProductName.ShouldBe(newProductCommand.Name);

    }


    [Fact]
    public async Task IntegrationEventRaisedByServiceADomainEventHandler_ShouldBeSuccuessfullyHandledByServiceB()
    {
        // (ShopApp) CreatePurchaseCommandHandler --> PurchaseCreatedDomainEvent
        // --> PurchaseCreatedDomainEventHandler --> ProductsPurchasedIntegrationEvent
        // ==> (ProductApp) ProductsPurchasedIntegrationEventHandler

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var purchaseCommand = new CreatePurchaseCommand(
            ProductPurchases: new List<ProductPurchase>
            {
                new ProductPurchase("Apples", 20),
                new ProductPurchase("Bananas", 15),
                new ProductPurchase("Pears", 11)
            }
        );

        // Act
        var purchaseResult = _shopServer.HandleCommand<CreatePurchaseCommand, Guid>(
            purchaseCommand, cancellationToken);

        // Assert
        await WaitForProductTotalSoldToBeNonZero(cancellationToken);

        var productData = await _productServer.HandleQuery<GetProductItemsByCategoryQuery, List<ProductItem>>(
            new GetProductItemsByCategoryQuery("Fruit"), cancellationToken);

        foreach (var productPurchased in purchaseCommand.ProductPurchases)
        {
            var productInfo = productData.Value.FirstOrDefault(p => p.Name == productPurchased.ProductName);           
            productInfo.ShouldNotBeNull();
            productInfo.TotalSold.ShouldBeGreaterThanOrEqualTo(productPurchased.PurchaseQuantity);
        }
    }
    [Fact]
    public async Task ChainOfMixedHandlersWithIntegrationEventOnServiceA_ShouldBeSuccuessfullyHandledByServiceB()
    {
        // (ShopApp) CreatePurchaseCommandHandler --> PurchaseCreatedDomainEvent
        // --> PurchaseCreatedDomainEventHandler --> LowStockDomainEvent
        // --> LowStockDomainEventHandler --> UpdateStockCommandHandler --> StockAddedIntegrationEvent
        // ==> (ProductApp) StockAddedIntegrationEventHandler

        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var initialStock = await _shopServer.HandleQuery<GetStockByProductNameQuery, ProductStock>(
            new GetStockByProductNameQuery("Apples"), cancellationToken);   

        var purchaseAllBut2ApplesCommand = new CreatePurchaseCommand(
            ProductPurchases: new List<ProductPurchase>
            {
                new ProductPurchase("Apples", initialStock.Value.TotalInStock-2)
            }
        );

        //Act
        var purchase = await _shopServer.HandleCommand<CreatePurchaseCommand, Guid>(
            purchaseAllBut2ApplesCommand, cancellationToken);

        var productInfo = await WaitForProductTotalShippedToIncrease(cancellationToken);

        // Assert
        productInfo.IsSuccess.ShouldBeTrue();
        productInfo.Value.ShouldNotBeNull();
        productInfo.Value.TotalShipped.ShouldBeGreaterThan(0);

    }

    private async Task<Result<ProductItem>> WaitForProductTotalShippedToIncrease(CancellationToken cancellationToken)
    {      
        var retryPolicy = Policy<Result<ProductItem>>
            .HandleResult(response => response.IsSuccess && response.Value.TotalShipped < 1)
            .WaitAndRetryAsync(50, _ => TimeSpan.FromMilliseconds(300));

        return await retryPolicy.ExecuteAsync(() =>
        {
            return _productServer.HandleQuery<GetProductItemByProductNameQuery, ProductItem>(
             new GetProductItemByProductNameQuery("Apples"), cancellationToken);
        });
    }

    private async Task<int> WaitForProductTotalSoldToBeNonZero(CancellationToken cancellationToken)
    {
        return await WaitForNonZeroResult(async () =>
        {
            var result = await _productServer.HandleQuery<GetProductItemByProductNameQuery, ProductItem>(
             new GetProductItemByProductNameQuery("Apples"), cancellationToken);
            if (result.IsSuccess && result.Value is not null)
            {
                return result.Value.TotalShipped;
            }
            return 0;
        });
    }

    private async Task<int> WaitForNonZeroResult(Func<Task<int>> action)
    {
        var retryPolicy = Policy<int>
            .HandleResult(result => result == 0)
            .WaitAndRetryAsync(50, _ => TimeSpan.FromMilliseconds(200));

        return await retryPolicy.ExecuteAsync(action);
    }

    private static Task<Result<T>> WaitForNonNullResult<T>(Func<Task<Result<T>>> action)
    {
        var retryPolicy = Policy<Result<T>>
            .HandleResult(response => response.IsNoContent() || response.IsNotFound() || response.Value is null)
            .WaitAndRetryAsync(50, _ => TimeSpan.FromMilliseconds(200));

        var result = retryPolicy.ExecuteAsync(action);

        return result!;
    }


}
