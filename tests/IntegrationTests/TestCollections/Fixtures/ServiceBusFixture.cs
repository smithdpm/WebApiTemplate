using Testcontainers.ServiceBus;

namespace IntegrationTests.TestCollections.Fixtures;
public class ServiceBusFixture : IAsyncLifetime
{
    private readonly ServiceBusContainer _serviceBusContainer = new ServiceBusBuilder()
    .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
    .WithConfig("service-bus-config.json")
    .WithAcceptLicenseAgreement(true)
    .Build();

    public async ValueTask DisposeAsync()
    {
        await _serviceBusContainer.DisposeAsync();
    }

    public async ValueTask InitializeAsync()
    {
        await _serviceBusContainer.StartAsync();
    }

    public string GetConnectionString()
    => _serviceBusContainer.GetConnectionString();
}
