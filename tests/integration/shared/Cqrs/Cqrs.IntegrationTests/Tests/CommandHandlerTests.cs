using Ardalis.Result;
using Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;
using Cqrs.IntegrationTests.Fixtures.ClassFixtures;
using Cqrs.Messaging;
using Cqrs.Outbox;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Polly;
using Shop.Application.Database;
using Shop.Application.Purchases;
using Shop.Application.Stock;
using Shouldly;

namespace Cqrs.IntegrationTests.Tests;

[Collection("CommandHandlerTests")]
public class CommandHandlerTests: IClassFixture<ShopAppFixture>
{
    private readonly TestServer _server;
    private readonly ShopAppFixture _shopApp;

    public CommandHandlerTests(DatabaseServerFixture databaseServerFixture
        , ShopAppFixture shopAppFixture)
    {
        _shopApp = shopAppFixture;
        _server = _shopApp.ShopApp.Server;
    }


    #region Validator tests
    [Fact]
    public async Task CommandHandlerWithValidator_ShouldReturnValidationError_WhenCalledWithInvalidCommand()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        using var scope = _server.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreatePurchaseCommand, Guid>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var command = new CreatePurchaseCommand(
            ProductPurchases: new List<ProductPurchase>
            {
                new ProductPurchase(string.Empty, 4),
                new ProductPurchase("Bananas", -2)
            }
        );

        // Act
        var result = await handler.HandleAsync(command, cancellationToken);

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

        using var scope = _server.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreatePurchaseCommand, Guid>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var command = new CreatePurchaseCommand(
            ProductPurchases: new List<ProductPurchase>
            {
                new ProductPurchase("Apples", 4),
                new ProductPurchase("Bananas", 2)
            }
        );

        // Act
        var result = await handler.HandleAsync(command, cancellationToken);

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

        using var scope = _server.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateStockCommand>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var command = new UpdateStockCommand(
            ProductName: "",
            QuantityToAdd: -5
        );

        // Act
        var result = await handler.HandleAsync(command, cancellationToken);

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

        using var scope = _server.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<UpdateStockCommand>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var command = new UpdateStockCommand(
            ProductName: "Apples",
            QuantityToAdd: 5
        );

        var initalQuantity = await dbContext.ProductStocks
            .AsNoTracking()
            .Where(p => p.ProductName == "Apples")
            .Select(a => a.TotalInStock).SingleAsync(cancellationToken);

        // Act
        var result = await handler.HandleAsync(command, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Status.ShouldBe(ResultStatus.Ok);
        var finalQuantity = await dbContext.ProductStocks
            .AsNoTracking()
            .Where(p => p.ProductName == "Apples")
            .Select(a => a.TotalInStock).SingleAsync(cancellationToken);

        finalQuantity.ShouldBe(initalQuantity + 5);
    }
    #endregion

    #region Domain event tests
    [Fact]
    public async Task CommandHandlerWithDomainEvent_ShouldExecuteDomainHanlder_WhenCalledWithValidCommand()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var scope = _server.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreatePurchaseCommand,Guid>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var command = new CreatePurchaseCommand(
            ProductPurchases: new List<ProductPurchase>
            {
                new ProductPurchase("Apples", 4),
                new ProductPurchase("Bananas", 2),
                new ProductPurchase("Kiwis", 1)
            }
        );

        var initialStock = await dbContext.ProductStocks
            .AsNoTracking()
            .Where(p=> p.ProductName == "Apples" || p.ProductName == "Bananas" || p.ProductName == "Kiwis")
            .ToListAsync(cancellationToken);

        var result = await handler.HandleAsync(command, cancellationToken);

        // Assert
        await WaitForOutboxToCompleteMessages(dbContext, cancellationToken);
        var finalStock = await dbContext.ProductStocks
            .AsNoTracking()
            .Where(p => p.ProductName == "Apples" || p.ProductName == "Bananas" || p.ProductName == "Kiwis")
            .ToListAsync(cancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        finalStock.Single(p => p.ProductName == "Apples").TotalInStock.ShouldBe(
            initialStock.SingleOrDefault(p => p.ProductName == "Apples")?.TotalInStock - 4 ?? -4
        );
    }
    #endregion

    #region Inter-service communication tests
    [Fact]
    public async Task CommandHandlerWithIntegrationEvent_ShouldExecuteIntegrationHanlder_WhenCalledWithValidCommand()
    {
    }
    #endregion

    private async Task<bool> OutboxMessagesStillPendingAsync(ApplicationDbContext applicationDbContext, CancellationToken cancellationToken)
    {
        var pendingMessages = await applicationDbContext.Set<OutboxMessage>()
            .AsNoTracking()
            .AnyAsync(om => !om.ProcessedAtUtc.HasValue, cancellationToken);
        return pendingMessages;
    }

    private Task WaitForOutboxToCompleteMessages(ApplicationDbContext applicationDbContext, CancellationToken cancellationToken)
    {
        var retryPolicy = Policy<bool>
            .Handle<HttpRequestException>()
            .OrResult(response => response)
            .WaitAndRetryAsync(50, _ => TimeSpan.FromMilliseconds(200));

        var result = retryPolicy.ExecuteAsync(
            () => OutboxMessagesStillPendingAsync(applicationDbContext, cancellationToken));

        return result!;
    }
}

