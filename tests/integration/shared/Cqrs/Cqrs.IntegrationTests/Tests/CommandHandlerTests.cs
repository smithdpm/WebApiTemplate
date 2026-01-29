using Cqrs.IntegrationTests.TestCollections.Environments;
using Cqrs.Messaging;
using Cqrs.Outbox;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Polly;
using Shop.Application.Database;
using Shop.Application.Purchases;
using Shouldly;

namespace Cqrs.IntegrationTests.Tests;


[Collection("IntegrationTestCollection")]
public class CommandHandlerTests
{
    private readonly IntegrationTestEnvironment _environment;
    private readonly HttpClient _host;
    private readonly TestServer _server;
    public CommandHandlerTests(IntegrationTestEnvironment environment)
    {
        _environment = environment;
        var dbConnectionString = _environment.Database.CreateDatabaseAsync("CommandHandlerTestsDb").GetAwaiter().GetResult();
        _environment.ShopApp.SetDatabaseConnectionString(dbConnectionString);
        _server = _environment.ShopApp.Server;
    }

    //[Fact]
    //public async Task WebCommandHandler_ShouldReturnResultSuccesfully_WhenCalledWithValidCommand()
    //{
    //    var cancellationToken = TestContext.Current.CancellationToken;
    //    var client = _environment.PrimaryApp.CreateClient();
    //    //var handler = _host.Services.GetRequiredService<ICommandHandler<AddItemCommand,Guid>>();
    //    //var result = await handler.Handle(new AddItemCommand("Test Item"), CancellationToken.None);
    //    var response = await client.PostAsJsonAsync("/items", new AddItemCommand("Test Item"), cancellationToken);
    //    using var createdJson = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);

    //    // Assert
    //    response.StatusCode.ShouldBe(HttpStatusCode.Created);
    //    createdJson!.RootElement.GetGuid().ShouldNotBe(Guid.Empty);

    //    //result.IsSuccess.ShouldBeTrue();
    //    //result.Value.ShouldNotBe(Guid.Empty);
    //}

    [Fact]
    public async Task CommandHandler_ShouldReturnResultSuccesfully_WhenCalledWithValidCommand()
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

        var result = await handler.Handle(command, cancellationToken);

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

