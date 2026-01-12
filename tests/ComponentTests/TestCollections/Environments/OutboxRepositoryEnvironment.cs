using ComponentTests.TestCollections.Fixtures;

namespace ComponentTests.TestCollections.Environments;
public class OutboxRepositoryEnvironment() : IAsyncLifetime
{
    public DatabaseFixture Database { get; private set; } = null!;
    public OutboxRepositoryFixture OutboxRepository { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        Database = new DatabaseFixture();
        OutboxRepository = new OutboxRepositoryFixture();

        await Database.InitializeAsync();

        OutboxRepository.SetDatabaseConnectionString(Database.GetConnectionString());
        await OutboxRepository.InitializeAsync();
    }
    public async ValueTask DisposeAsync()
    {
        await OutboxRepository.DisposeAsync();
        await Database.DisposeAsync();
    }
}
