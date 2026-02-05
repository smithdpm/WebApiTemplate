using Microsoft.AspNetCore.Mvc.Testing;

namespace Cqrs.IntegrationTests.AppServices;

public class TestApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private string? _databaseConnectionString;
    private string? _serviceBusConnectionString;

    public void SetDatabaseConnectionString(string connectionString)
    => _databaseConnectionString = connectionString;
    public void SetServiceBusConnectionString(string connectionString)
        => _serviceBusConnectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings:Database", _databaseConnectionString);
        Environment.SetEnvironmentVariable("AzureServiceBus:ConnectionString", _serviceBusConnectionString);
    }
}
