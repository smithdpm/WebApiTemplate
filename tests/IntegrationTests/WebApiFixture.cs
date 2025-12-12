
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;


namespace IntegrationTests;
public class WebApiFixture: WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-CU10-ubuntu-22.04")
        .Build();

    private readonly ServiceBusContainer _serviceBusContainer = new ServiceBusBuilder()
        .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
        .WithConfig("service-bus-config.json")
        .WithAcceptLicenseAgreement(true)
        .Build();

    public ITestOutputHelper? OutputHelper { get; set; }



    public void ClearOutputHelper()
        => OutputHelper = null;
    public void SetOutputHelper(ITestOutputHelper value)
        => OutputHelper = value;

    public string GetServiceBusConnectionString()
        => _serviceBusContainer.GetConnectionString();

    public new async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _serviceBusContainer.DisposeAsync();
    }
    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        var containerTasks = new List<Task>
        {
            _dbContainer.StartAsync(),
            _serviceBusContainer.StartAsync()
        };
        await Task.WhenAll(containerTasks);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("UseOnlyInMemoryDatabase", "false");
        Environment.SetEnvironmentVariable("ConnectionStrings:Database", _dbContainer.GetConnectionString());

        Environment.SetEnvironmentVariable("AzureServiceBus:ConnectionString", _serviceBusContainer.GetConnectionString());
    }


}
