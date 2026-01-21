using Cqrs.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Cqrs.IntegrationTests.TestCollections.Fixtures;

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

        // Use reflection to register the internal OutboxRepository
        services.AddSingleton<IOutboxRepository>(provider =>
        {
            var dbContextFactory = provider.GetRequiredService<IDbContextFactory<TestOutboxContext>>();
            var dbContext = dbContextFactory.CreateDbContext();
            
            // Get the assembly containing OutboxRepository
            var cqrsAssembly = Assembly.GetAssembly(typeof(Cqrs.EntityFrameworkCore.DependancyInjection));
            var outboxRepositoryType = cqrsAssembly!.GetType("Cqrs.EntityFrameworkCore.Database.OutboxRepository`1");
            var genericType = outboxRepositoryType!.MakeGenericType(typeof(TestOutboxContext));
            
            return (IOutboxRepository)Activator.CreateInstance(genericType, dbContext)!;
        });


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