using Cqrs.IntegrationTests.AppServices;
using Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;

namespace Cqrs.IntegrationTests.Fixtures.ClassFixtures;

public class MultiAppFixture : IAsyncLifetime
{
    public TestApplicationFactory<ShopAppProgram> ShopAppFactory { get; private set; }
    public TestApplicationFactory<ProductAppProgram> ProductAppFactory { get; private set; }
    private DatabaseServerFixture _databaseServerFixture;
    private ServiceBusFixture _serviceBusFixture;
    private string _shopDatabaseName;
    private string _productDatabaseName;

    public MultiAppFixture(DatabaseServerFixture databaseServerFixture
        ,ServiceBusFixture serviceBusFixture)
    {
        _databaseServerFixture = databaseServerFixture;
        _serviceBusFixture = serviceBusFixture;
        ShopAppFactory = new TestApplicationFactory<ShopAppProgram>();
        ProductAppFactory = new TestApplicationFactory<ProductAppProgram>();
        _shopDatabaseName = $"ShopAppDb_{Guid.CreateVersion7()}";
        _productDatabaseName = $"ProductAppDb_{Guid.CreateVersion7()}";
    }
    public async ValueTask InitializeAsync()
    {
        var shopDbConnectionString = await _databaseServerFixture.CreateDatabaseAsync(_shopDatabaseName);
        var prodcutDbConnectionString = await _databaseServerFixture.CreateDatabaseAsync(_productDatabaseName);
        ShopAppFactory.SetDatabaseConnectionString(shopDbConnectionString);
        ProductAppFactory.SetDatabaseConnectionString(prodcutDbConnectionString);
        
        var serviveBusConnectionString = _serviceBusFixture.GetConnectionString();
        ShopAppFactory.SetServiceBusConnectionString(serviveBusConnectionString);
        ProductAppFactory.SetServiceBusConnectionString(serviveBusConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        ShopAppFactory.Dispose();
        ProductAppFactory.Dispose();
        await _databaseServerFixture.DropDatabaseAsync(_shopDatabaseName);
        await _databaseServerFixture.DropDatabaseAsync(_productDatabaseName);
    }
}
