using Cqrs.Events.IntegrationEvents;

namespace Cqrs.ApplicationTestFixture;

public class TestIntegrationEventHandler : IntegrationEventHandler<TestIntegrationEvent>
{
    public override Task HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}