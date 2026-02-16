using Cqrs.IntegrationTests.AppServices;
using Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;

namespace Cqrs.IntegrationTests.Fixtures.ClassFixtures;

public class ShopAppFixture : IAsyncLifetime
{
    public TestApplicationFactory<ShopAppProgram> ShopApp { get; private set; }
    private DatabaseServerFixture _databaseServerFixture;
    private string _databaseName;

    public ShopAppFixture(DatabaseServerFixture databaseServerFixture)
    {
        _databaseServerFixture = databaseServerFixture;
        ShopApp = new TestApplicationFactory<ShopAppProgram>();
        _databaseName = $"ShopAppDb_{Guid.CreateVersion7()}";
    }
    public async ValueTask InitializeAsync()
    {
        var dbConnectionString = await _databaseServerFixture.CreateDatabaseAsync(_databaseName);
        ShopApp.SetDatabaseConnectionString(dbConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        ShopApp.Dispose();
        await _databaseServerFixture.DropDatabaseAsync(_databaseName);
    }
}
