using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;

namespace Cqrs.ApplicationTestFixture;

public class TestIntegrationEventHandler : IntegrationEventHandler<TestIntegrationEvent>
{
    public override Task<Result> HandleAsync(TestIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}