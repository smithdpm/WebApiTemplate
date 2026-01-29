
using Cqrs.IntegrationTests.TestCollections.Fixtures;

namespace Cqrs.IntegrationTests.TestCollections.Environments;

public class IntegrationTestEnvironment() : IAsyncLifetime
{
    public DatabaseFixture Database { get; private set; } = null!;
    public ServiceBusFixture ServiceBus { get; private set; } = null!;
    public ShopApp ShopApp { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        Database = new DatabaseFixture();
        ServiceBus = new ServiceBusFixture();
        ShopApp = new ShopApp();

        await Database.InitializeAsync();
        //await ServiceBus.InitializeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await ShopApp.DisposeAsync();
        await Database.DisposeAsync();
        //await ServiceBus.DisposeAsync();
    }



}
