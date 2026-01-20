using Cqrs.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Cqrs.EntityFrameworkCore.Database;

namespace ComponentTests.TestCollections.Fixtures;

public sealed class OutboxRepositoryFixture : IAsyncLifetime
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    private string? _databaseConnectionString;

    public void SetDatabaseConnectionString(string connectionString)
        => _databaseConnectionString = connectionString;
    public async ValueTask DisposeAsync()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public async ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContextFactory<TestOutboxContext>(options =>
                {
                    options.UseSqlServer(_databaseConnectionString);
                });

        services.AddScoped<IOutboxRepository, OutboxRepository<TestOutboxContext>>();


        ServiceProvider = services.BuildServiceProvider();

        using var scope = ServiceProvider.CreateScope();
        using var context = await CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task<TestOutboxContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var contextFactory = ServiceProvider.GetRequiredService<IDbContextFactory<TestOutboxContext>>();
        return await contextFactory.CreateDbContextAsync(cancellationToken);
    }

    public async Task CleanDatabaseAsync()
    {
        using var context = await CreateDbContextAsync();
        context.Database.ExecuteSqlRaw("TRUNCATE TABLE outbox.OutboxMessages");
        await context.SaveChangesAsync();
    }

    public async Task<bool> CheckAllMessagesHaveBeenProcessedAsync()
    {
        using var context = await CreateDbContextAsync();
        var countOfUnprocessedMessages = context.OutboxMessages.AsNoTracking()
            .Where(m => !m.ProcessedAtUtc.HasValue)
            .Count();

        return countOfUnprocessedMessages == 0;
    }
}