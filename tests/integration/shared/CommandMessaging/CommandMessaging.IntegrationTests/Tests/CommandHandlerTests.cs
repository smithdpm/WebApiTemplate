using Ardalis.Result;
using Cqrs.IntegrationTests.Extensions;
using Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;
using Cqrs.IntegrationTests.Fixtures.ClassFixtures;
using Cqrs.Operations.Commands;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Database;
using Shop.Application.Purchases;
using Shop.Application.Stock;
using Shop.Domain.Aggregates.Stock;
using Shouldly;

namespace Cqrs.IntegrationTests.Tests;

[Collection("CommandHandlerTests")]
public class CommandHandlerTests : IClassFixture<ShopAppFixture>
{
    private readonly TestServer _shopApp;
    private readonly ShopAppFixture _shopAppFixture;

    public CommandHandlerTests(DatabaseServerFixture databaseServerFixture
        , ShopAppFixture shopAppFixture)
    {
        _shopAppFixture = shopAppFixture;
        _shopApp = _shopAppFixture.ShopApp.Server;
    }


    #region Validator tests
    [Fact]
    public async Task CommandHandlerWithValidator_ShouldReturnValidationError_WhenCalledWithInvalidCommand()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _shopApp.HandleCommand<CreatePurchaseCommand, Guid>(
            new CreatePurchaseCommand(
                ProductPurchases: new List<ProductPurchase>
                {
                    new ProductPurchase(string.Empty, 4),
                    new ProductPurchase("Bananas", -2)
                }), 
            cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(
            ve => ve.Identifier == "ProductPurchases[0].ProductName" && ve.ErrorMessage.Contains("Product name must not be empty")
        );
        result.ValidationErrors.ShouldContain(
            ve => ve.Identifier == "ProductPurchases[1].PurchaseQuantity" && ve.ErrorMessage.Contains("Purchase quantity must be greater than zero.")
        );
    }

    [Fact]
    public async Task CommandHandlerWithValidator_ShouldExecuteSuccessfully_WhenCalledWithValidCommand()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _shopApp.HandleCommand<CreatePurchaseCommand, Guid>(
            new CreatePurchaseCommand(
                ProductPurchases: new List<ProductPurchase>
                {
                    new ProductPurchase("Apples", 4),
                    new ProductPurchase("Bananas", 2)
                }), 
            cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Created);
        result.Value.ShouldBeOfType<Guid>();
        result.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task NoResultCommandHandlerWithValidator_ShouldReturnValidationError_WhenCalledWithInvalidCommand()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _shopApp.HandleCommand(
            new UpdateStockCommand(
                ProductName: "",
                QuantityToAdd: -5), 
            cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Status.ShouldBe(ResultStatus.Invalid);
        result.ValidationErrors.ShouldContain(
            ve => ve.Identifier == "ProductName" && ve.ErrorMessage.Contains("Product name must not be empty")
        );
        result.ValidationErrors.ShouldContain(
            ve => ve.Identifier == "QuantityToAdd" && ve.ErrorMessage.Contains("Quantity to add must be greater than zero.")
        );
    }

    [Fact]
    public async Task NoResultCommandHandlerWithValidator_ShouldExecuteSuccessfully_WhenCalledWithValidCommand()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        using var scope = _shopApp.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateStockCommand>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var command = new UpdateStockCommand(
            ProductName: "Apples",
            QuantityToAdd: 5
        );

        var stockBeforeUpdate = await _shopApp.HandleQuery<GetStockByProductNameQuery, ProductStock>(
            new GetStockByProductNameQuery("Apples"), cancellationToken);   

        // Act
        var result = await _shopApp.HandleCommand(
            new UpdateStockCommand(
                ProductName: "Apples",
                QuantityToAdd: 5),
            cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Ok);

        var stockAfterUpdate = await _shopApp.HandleQuery<GetStockByProductNameQuery, ProductStock>(
            new GetStockByProductNameQuery("Apples"), cancellationToken);

        stockAfterUpdate.Value.TotalInStock.ShouldBe(stockBeforeUpdate.Value.TotalInStock + 5);
    }
    #endregion

    #region Domain event tests
    [Fact]
    public async Task CommandHandlerWithDomainEvent_ShouldExecuteDomainHanlder_WhenCalledWithValidCommand()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var scope = _shopApp.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreatePurchaseCommand, Guid>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var purchaseCommand = new CreatePurchaseCommand(
            ProductPurchases: new List<ProductPurchase>
            {
                new ProductPurchase("Apples", 4),
                new ProductPurchase("Bananas", 2),
                new ProductPurchase("Kiwis", 1)
            }
        );

        var stockBeforePurchase = await _shopApp.HandleQuery<GetAllStockQuery, List<ProductStock>>(
            new GetAllStockQuery(50), cancellationToken);

        var result = await _shopApp.HandleCommand<CreatePurchaseCommand, Guid>(
            purchaseCommand,
            cancellationToken);

        // Assert
        await _shopApp.WaitForOutboxToCompleteMessages<ApplicationDbContext>(cancellationToken);

        var stockAfterPurchase = await _shopApp.HandleQuery<GetAllStockQuery, List<ProductStock>>(
            new GetAllStockQuery(50), cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        foreach (var productPurhase in purchaseCommand.ProductPurchases)
        {
            var stockAmountBeforePurchase = stockBeforePurchase.Value
                .Where(p => p.ProductName == productPurhase.ProductName)
                .First().TotalInStock;
            
            var stockAmountAfterPurchase = stockAfterPurchase.Value
                .Where(p => p.ProductName == productPurhase.ProductName)
                .First().TotalInStock;

            productPurhase.PurchaseQuantity.ShouldBe(stockAmountBeforePurchase - stockAmountAfterPurchase);
        }
    }
    #endregion

    #region Inter-service communication tests
    [Fact]
    public async Task CommandHandlerWithIntegrationEvent_ShouldExecuteIntegrationHanlder_WhenCalledWithValidCommand()
    {
    }
    #endregion


}

