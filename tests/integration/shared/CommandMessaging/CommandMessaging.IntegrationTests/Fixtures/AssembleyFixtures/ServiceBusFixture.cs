using Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;
using Testcontainers.ServiceBus;

[assembly: AssemblyFixture(typeof(ServiceBusFixture))]
namespace Cqrs.IntegrationTests.Fixtures.AssembleyFixtures;
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
