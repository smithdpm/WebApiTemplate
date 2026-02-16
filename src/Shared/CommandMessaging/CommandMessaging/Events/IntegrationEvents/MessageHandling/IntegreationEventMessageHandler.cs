using Azure.Messaging;
using Cqrs.Decorators.Registries;
using Cqrs.Events.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace Cqrs.Events.IntegrationEvents.MessageHandling;

public interface IMessageHandler
{
    Task<MessageResult> HandleMessageAsync(string messageId, BinaryData messageBody, CancellationToken cancellationToken);
}

internal class IntegreationEventMessageHandler: IMessageHandler
{
    protected readonly ILogger<IntegreationEventMessageHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventTypeRegistry _integrationEventTypeRegistry;

    public IntegreationEventMessageHandler(ILogger<IntegreationEventMessageHandler> logger, 
        IServiceScopeFactory scopeFactory,
        IEventTypeRegistry integrationEventTypeRegistry)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _integrationEventTypeRegistry = integrationEventTypeRegistry;
    }

    public async Task<MessageResult> HandleMessageAsync(string messageId, BinaryData messageBody, CancellationToken cancellationToken)
    {
        var parseCloudEventResult = ParseCloudEvent(messageBody, messageId);

        if (!parseCloudEventResult.IsSuccess)
            return parseCloudEventResult.ToMessageResult();


        var cloudEvent = parseCloudEventResult.Value;

        var integrationEventResult = ParseIntegrationEvent(cloudEvent, messageId);

        if (!integrationEventResult.IsSuccess)
            return integrationEventResult.ToMessageResult();


        _logger.LogInformation("Handling event {EventId} of type {EventType}", cloudEvent.Id, cloudEvent.Type);

        var dispatchToHandlersResult = await DispatchEventToEventHandlers(integrationEventResult.Value, messageId, cancellationToken);
        if (!dispatchToHandlersResult.IsSuccess)
            return dispatchToHandlersResult.ToMessageResult();

        _logger.LogInformation($"Successfully processed message {messageId} for event {cloudEvent.Id} of type {cloudEvent.Type}.");

        return dispatchToHandlersResult.ToMessageResult();
    }
    private MessageStepResult<IIntegrationEvent> ParseIntegrationEvent(CloudEvent cloudEvent, string messageId)
    {
        var eventType = _integrationEventTypeRegistry.GetTypeByName(cloudEvent.Type);

        if (eventType == null)
        {
            _logger.LogWarning($"Skipping message {messageId}: UnregisteredIntegrationEventType - The integration event type {cloudEvent.Type} is not a registered integration event for this service.");
            return MessageStepResult<IIntegrationEvent>.Skip(
                "UnregisteredIntegrationEventType",
                $"The integration event type {cloudEvent.Type} is not a registered integration event for this service."
                );
        }

        try
        {
            var integrationEvent = JsonSerializer.Deserialize(
                    cloudEvent.Data!.ToMemory().Span,
                    eventType) as IIntegrationEvent;

            if (integrationEvent == null)
            {
                _logger.LogError($"Dead-lettering message {messageId}: DeserializationResultedInNull - Failed to deserialize CloudEvent data to type {eventType.Name}.");

                return MessageStepResult<IIntegrationEvent>.DeadLetter("DeserializationResultedInNull",
                    $"Failed to deserialize CloudEvent data to type {eventType.Name}.");
            }
            return MessageStepResult<IIntegrationEvent>.Success(integrationEvent);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError($"Dead-lettering message {messageId}: InvalidJsonFormat - JSON Deserialization failed for integration event type {cloudEvent.Type} event {cloudEvent.Id}. ExceptionMessage: {jsonEx.Message}");

            return MessageStepResult<IIntegrationEvent>.DeadLetter(
                "InvalidJsonFormat",
                $"JSON Deserialization failed for integration event type {cloudEvent.Type} event {cloudEvent.Id}. ExceptionMessage: {jsonEx.Message}"
                );
        }
    }

    private async Task<MessageStepResult<bool>> DispatchEventToEventHandlers(
        IIntegrationEvent integrationEvent, 
        string messageId,
        CancellationToken cancellationToken)
    {
        var eventType = integrationEvent.GetType();

        using var scope = _scopeFactory.CreateAsyncScope();

        var handlers = scope.ServiceProvider
                    .GetServices(typeof(IIntegrationEventHandler<>).MakeGenericType(eventType))
                    .Cast<IIntegrationEventHandler>();

        if (!handlers.Any())
        {
            _logger.LogWarning($"Skipping message {messageId}: NoRegisteredHandlers - No handlers registered for event type {eventType}.");
            return MessageStepResult<bool>.Skip(
                "NoRegisteredHandlers",
                $"No handlers registered for event type {eventType}.");
        }

        foreach (var handler in handlers)
        {
            if (handler == null) continue;

            await handler.HandleAsync(integrationEvent, cancellationToken);
        }

        return MessageStepResult<bool>.Success(true);
    }


    private MessageStepResult<CloudEvent> ParseCloudEvent(BinaryData data, string messageId)
    {
        var cloudEvent = CloudEvent.Parse(data);

        if (cloudEvent?.Data is null || string.IsNullOrEmpty(cloudEvent?.Type))
        {
            _logger.LogError($"Dead-lettering message {messageId}: InvalidCloudEvent - Received message is not a valid CloudEvent with a Type and Data.");
            return MessageStepResult<CloudEvent>.DeadLetter("InvalidCloudEvent", "Received message is not a valid CloudEvent with a Type and Data.");
        }
        return MessageStepResult<CloudEvent>.Success(cloudEvent);
    }


}

