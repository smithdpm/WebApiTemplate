using Cqrs.Events.IntegrationEvents;

namespace Cqrs.ApplicationTestFixture;

public record TestIntegrationEvent : IntegrationEventBase
{
    public Guid EventId { get; init; }
}