using Testcontainers.MsSql;

namespace WebApiTemplate.IntegrationTests.TestCollections.Fixtures;


public sealed class DatabaseFixture()
    : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-CU10-ubuntu-22.04")
        .Build();

    public string GetConnectionString()
    => _dbContainer.GetConnectionString();
   
    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }
    public async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
    }
}
