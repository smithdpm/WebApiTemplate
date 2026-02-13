using Ardalis.Result;
using Cqrs.Messaging;
using Cqrs.Outbox;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Cqrs.IntegrationTests.Extensions;

public static class TestServerExtensions
{
    public static async Task<Result<TResponse>> HandleCommand<TCommand, TResponse>(
        this TestServer testServer, 
        TCommand command, 
        CancellationToken cancellationToken)
        where TCommand : ICommand<TResponse>
    {
        using var productScope = testServer.Services.CreateScope();

        var handler = productScope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();

        return await handler.HandleAsync(command, cancellationToken);
    }

    public static async Task<Result> HandleCommand<TCommand>(
        this TestServer testServer,
        TCommand command,
        CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        using var productScope = testServer.Services.CreateScope();

        var handler = productScope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand>>();

        return await handler.HandleAsync(command, cancellationToken);
    }

    public static async Task<Result<TResponse>> HandleQuery<TQuery, TResponse>(
        this TestServer testServer,
        TQuery query,
        CancellationToken cancellationToken)
        where TQuery : IQuery<TResponse>
    {
        using var productScope = testServer.Services.CreateScope();

        var handler = productScope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();

        return await handler.HandleAsync(query, cancellationToken);
    }

    public static async Task<bool> WaitForOutboxToCompleteMessages<TDbContext>(this TestServer testServer, CancellationToken cancellationToken)
        where TDbContext: DbContext
    {
        using var scope = testServer.Services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();

        var retryPolicy = Policy<bool>
            .Handle<HttpRequestException>()
            .OrResult(response => response)
            .WaitAndRetryAsync(50, _ => TimeSpan.FromMilliseconds(200));

        return await retryPolicy.ExecuteAsync(() =>
        {
            return dbContext.Set<OutboxMessage>()
            .AsNoTracking()
            .AnyAsync(om => !om.ProcessedAtUtc.HasValue, cancellationToken);
        });
    }
}
