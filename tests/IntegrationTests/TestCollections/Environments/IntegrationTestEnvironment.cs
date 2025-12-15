using IntegrationTests.TestCollections.Fixtures;

namespace IntegrationTests.TestCollections.Environments;
public class IntegrationTestEnvironment() : IAsyncLifetime
{
    public DatabaseFixture Database { get; private set; } = null!;
    public ServiceBusFixture ServiceBus { get; private set; } = null!;
    public WebApiFixture WebApi { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {        
        Database = new DatabaseFixture();
        ServiceBus = new ServiceBusFixture();
        WebApi = new WebApiFixture();

        await Database.InitializeAsync();
        await ServiceBus.InitializeAsync(); 


        WebApi.SetDatabaseConnectionString(Database.GetConnectionString());
        WebApi.SetServiceBusConnectionString(ServiceBus.GetConnectionString());
    }
    public async ValueTask DisposeAsync()
    {
        await WebApi.DisposeAsync();
        await Database.DisposeAsync();
    }
}
