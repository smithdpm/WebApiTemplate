using Cqrs.IntegrationTests.AppServices;
using Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;
using Microsoft.Data.SqlClient;
using Respawn;

namespace Cqrs.IntegrationTests.Fixtures.ClassFixtures;

public class MultiAppFixture : IAsyncLifetime
{
    public TestApplicationFactory<ShopAppProgram> ShopAppFactory { get; private set; }
    public TestApplicationFactory<ProductAppProgram> ProductAppFactory { get; private set; }
    private DatabaseServerFixture _databaseServerFixture;
    private ServiceBusFixture _serviceBusFixture;
    private string _shopDatabaseName;
    private string _productDatabaseName;
    private Respawner _shopRespawner = default!;
    private Respawner _productRespawner = default!;

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

        var initialiseShopServer = ShopAppFactory.Server;
        var initialiseProductServer = ProductAppFactory.Server;

        _shopRespawner =  await _databaseServerFixture.GetRespawnerAsync(_shopDatabaseName);
        _productRespawner =  await _databaseServerFixture.GetRespawnerAsync(_productDatabaseName);
    }

    public async ValueTask DisposeAsync()
    {
        ShopAppFactory.Dispose();
        ProductAppFactory.Dispose();
        await _databaseServerFixture.DropDatabaseAsync(_shopDatabaseName);
        await _databaseServerFixture.DropDatabaseAsync(_productDatabaseName);
    }
    public async Task ReseedDatabases()
    {
        await Task.WhenAll(ReseedShopDatabase(), ReseedProductDatabase());
    }

    public async Task ReseedShopDatabase()
    {
        using var connection = new SqlConnection(_databaseServerFixture.GetDatabaseConnectionString(_shopDatabaseName));
        await connection.OpenAsync();
        await _shopRespawner.ResetAsync(connection);
        
        var app = ShopAppFactory.Server;
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<Shop.Application.Database.ApplicationDbContext>();

        await Shop.Application.Database.DatabaseExtensions.SeedAsync(dbContext);
    }
    public async Task ReseedProductDatabase()
    {
        using var connection = new SqlConnection(_databaseServerFixture.GetDatabaseConnectionString(_productDatabaseName));
        await connection.OpenAsync();
        await _productRespawner.ResetAsync(connection);

        var app = ProductAppFactory.Server;
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<Product.App.Database.ApplicationDbContext>();

        await Product.App.Database.DatabaseExtensions.SeedAsync(dbContext);
    }

}
