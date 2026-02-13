
using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.Events.IntegrationEvents;
public interface IIntegrationEventHandler<TEvent>: IIntegrationEventHandler, IHandler<TEvent, Result>
    where TEvent : IIntegrationEvent;

public interface IIntegrationEventHandler
{
    Type EventType { get; }
    Task<Result> HandleAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);
}