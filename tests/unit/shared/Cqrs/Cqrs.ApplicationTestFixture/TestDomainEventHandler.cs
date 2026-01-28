using Cqrs.DomainTestFixture;
using Cqrs.Events.DomainEvents;

namespace Cqrs.ApplicationTestFixture;

public class TestDomainEventHandler : DomainEventHandler<TestDomainEvent>
{
    public override Task HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}