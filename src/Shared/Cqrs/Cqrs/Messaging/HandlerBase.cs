using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cqrs.Messaging;

public abstract class HandlerBase<TInput, TResult> : IHandler<TInput, TResult>
    where TResult : IResult
{
    private Dictionary<string, List<IntegrationEventBase>> _integrationEvents = new();
    [NotMapped]
    public IReadOnlyDictionary<string, List<IntegrationEventBase>> IntegrationEventsToSend => _integrationEvents.AsReadOnly();

    public void AddIntegrationEvent(string destination, IntegrationEventBase eventItem)
    {
        if (!_integrationEvents.ContainsKey(destination))
            _integrationEvents[destination] = new List<IntegrationEventBase>();
        _integrationEvents[destination].Add(eventItem);
    }

    public void ClearIntegrationEvents() => _integrationEvents.Clear();

    public abstract Task<TResult> HandleAsync(TInput input, CancellationToken cancellationToken);
}
