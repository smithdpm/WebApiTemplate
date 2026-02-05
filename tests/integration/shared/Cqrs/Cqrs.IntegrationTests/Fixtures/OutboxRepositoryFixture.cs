using Cqrs.EntityFrameworkCore.Database;
using Cqrs.IntegrationTests.DbContexts;
using Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;
using Cqrs.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.IntegrationTests.Fixtures;

public sealed class OutboxRepositoryFixture : IAsyncLifetime
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    private DatabaseServerFixture _databaseServerFixture; 
    private string _databaseName;

    public OutboxRepositoryFixture(DatabaseServerFixture databaseServerFixture)
    {
        _databaseServerFixture = databaseServerFixture;
        _databaseName = $"OutboxTestDb_{Guid.CreateVersion7()}";
    }
    public async ValueTask DisposeAsync()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        await _databaseServerFixture.DropDatabaseAsync(_databaseName);
    }

    public async ValueTask InitializeAsync()
    {
        var databaseConnectionString = await _databaseServerFixture.CreateDatabaseAsync(_databaseName);
        var services = new ServiceCollection();

        services.AddDbContextFactory<TestOutboxContext>(options =>
                {
                    options.UseSqlServer(databaseConnectionString);
                });

        services.AddSingleton<IOutboxRepository, OutboxRepository<TestOutboxContext>>();

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