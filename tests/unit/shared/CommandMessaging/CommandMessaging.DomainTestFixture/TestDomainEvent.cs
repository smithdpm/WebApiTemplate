using SharedKernel.Events;

namespace Cqrs.DomainTestFixture;

public record TestDomainEvent : IDomainEvent
{
    public Guid EntityId { get; init; }
}