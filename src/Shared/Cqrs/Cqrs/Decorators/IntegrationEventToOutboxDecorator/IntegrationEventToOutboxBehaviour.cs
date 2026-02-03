
using Ardalis.Result;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Extensions;
using Cqrs.Messaging;
using Cqrs.Outbox;
using SharedKernel.Database;

namespace Cqrs.Decorators.IntegrationEventToOutboxDecorator;

public class IntegrationEventToOutboxBehaviour(
    IRepository<OutboxMessage> repository) : IIntegrationEventToOutboxBehaviour
{
    public async Task<TResult> ExecuteAsync<TInput, TResult>(HandlerBase<TInput, TResult> handler, TInput input, CancellationToken cancellationToken)
        where TResult : IResult
    {
        var result = await handler.HandleAsync(input, cancellationToken);

        if (result.IsSuccess() && handler.IntegrationEventsToSend.Count > 0)
        {
            await repository.AddRangeAsync(
                IntegrationEventExtensions.IntegrationEventsToOutboxMessages(handler.IntegrationEventsToSend));
        }

        return result;
    }
}
