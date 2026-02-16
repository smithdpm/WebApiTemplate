using Ardalis.Result;
using Cqrs.DomainTestFixture;
using Cqrs.Events.DomainEvents;

namespace Cqrs.ApplicationTestFixture;

public class TestDomainEventHandler : DomainEventHandler<TestDomainEvent>
{
    public override Task<Result> HandleAsync(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success());
    }
}