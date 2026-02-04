using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;


namespace WebApiTemplate.IntegrationTests.TestCollections.Fixtures;
public class WebApiFixture(): WebApplicationFactory<Program>
{

    public ITestOutputHelper? OutputHelper { get; set; }
    private string? _databaseConnectionString;
    private string? _serviceBusConnectionString;

    public void ClearOutputHelper()
        => OutputHelper = null;
    public void SetOutputHelper(ITestOutputHelper value)
        => OutputHelper = value;

    public void SetDatabaseConnectionString(string connectionString)
        => _databaseConnectionString = connectionString;
    public void SetServiceBusConnectionString(string connectionString)
        => _serviceBusConnectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {       
        Environment.SetEnvironmentVariable("UseOnlyInMemoryDatabase", "false");
        Environment.SetEnvironmentVariable("ConnectionStrings:Database", _databaseConnectionString);
        Environment.SetEnvironmentVariable("CqrsSettings:AzureServiceBus:ConnectionString", _serviceBusConnectionString);
    }


}