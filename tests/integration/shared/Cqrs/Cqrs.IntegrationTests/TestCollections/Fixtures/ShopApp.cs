using Microsoft.AspNetCore.Mvc.Testing;
using System.Reflection;

namespace Cqrs.IntegrationTests.TestCollections.Fixtures;


public class ShopApp: WebApplicationFactory<Program>
{
    private string? _databaseConnectionString;
    private string? _serviceBusConnectionString;

    public void SetDatabaseConnectionString(string connectionString)
    => _databaseConnectionString = connectionString;
    public void SetServiceBusConnectionString(string connectionString)
        => _serviceBusConnectionString = connectionString;
    protected override IEnumerable<Assembly> GetTestAssemblies() =>
       new[] { typeof(ShopApp).Assembly };
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings:Database", _databaseConnectionString);
        Environment.SetEnvironmentVariable("AzureServiceBus:ConnectionString", _serviceBusConnectionString);
    }
}
