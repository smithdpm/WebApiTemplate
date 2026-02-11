using Azure.Messaging;
using Azure.Messaging.ServiceBus;
using Cqrs.Decorators.Registries;
using Cqrs.Events.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;


namespace Cqrs.AzureServiceBus.Reciever.Subscriber;

public class TopicSubscriber : BackgroundService
{
    protected readonly ILogger<TopicSubscriber> _logger;
    protected readonly ServiceBusProcessor _processor;
    private readonly TopicSubscriberSettings _subscriptionSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEventTypeRegistry _integrationEventTypeRegistry;

    public TopicSubscriber(ILogger<TopicSubscriber> logger,
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory,
        IEventTypeRegistry integrationEventTypeRegistry,
        TopicSubscriberSettings topicSubscriberSettings)
    {
        _logger = logger;
        _subscriptionSettings = topicSubscriberSettings;
        _scopeFactory = scopeFactory;
        _integrationEventTypeRegistry = integrationEventTypeRegistry;
        _processor = serviceBusClient.CreateProcessor(topicSubscriberSettings.TopicName,
            topicSubscriberSettings.SubscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = topicSubscriberSettings.MaxConcurrentCalls
            });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Starting Service Bus Topic Subscriber: TopicName ({_subscriptionSettings.TopicName}), SubscriptionName ({_subscriptionSettings.SubscriptionName})");

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation(
            $"Stopping Service Bus Topic Subscriber: TopicName ({_subscriptionSettings.TopicName}), SubscriptionName ({_subscriptionSettings.SubscriptionName})");

        await _processor.StopProcessingAsync(stoppingToken);
    }
    protected async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        var parseCloudEventResult = ParseCloudEvent(args.Message.Body);

        if (!parseCloudEventResult.IsSuccess)
        {
            await FinalizeMessageProcessingAsync(args, parseCloudEventResult);
            return;
        }

        var cloudEvent = parseCloudEventResult.Value;

        var integrationEventResult = ParseIntegrationEvent(cloudEvent);

        if (!integrationEventResult.IsSuccess)
        {
            await FinalizeMessageProcessingAsync(args, integrationEventResult, cloudEvent);
            return;
        }

        _logger.LogInformation("Handling event {EventId} of type {EventType}", cloudEvent.Id, cloudEvent.Type);

        try
        {
            var dispatchToHandlersResult = await DispatchEventToEventHandlers(integrationEventResult.Value);
            if (!dispatchToHandlersResult.IsSuccess)
            {
                await FinalizeMessageProcessingAsync(args, integrationEventResult, cloudEvent);
                return;
            }

            await FinalizeMessageProcessingAsync(args, dispatchToHandlersResult, cloudEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to process CloudEvent. Message will be retried.");
        }
    }

    private async Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            $"An error occured while running the Service Bus Topic Subscriber: TopicName ({_subscriptionSettings.TopicName}), SubscriptionName ({_subscriptionSettings.SubscriptionName})");
    }

    private async Task FinalizeMessageProcessingAsync<T>(ProcessMessageEventArgs args, MessageStepResult<T> stepResult, CloudEvent? cloudEvent = null)
    {
        switch (stepResult.Status)
        {
            case MessageStepStatus.Success:
                if (cloudEvent != null)
                {
                    _logger.LogInformation("Successfully processed message {MessageId} for event {EventId} of type {EventType}.", args.Message.MessageId, cloudEvent.Id, cloudEvent.Type);
                }
                else
                {
                    _logger.LogInformation("Successfully processed message {MessageId}.", args.Message.MessageId);
                }
                await args.CompleteMessageAsync(args.Message);
                break;
            case MessageStepStatus.DeadLetter:
                string deadletterPrefix = "Dead-lettering message {MessageId}";
                if (cloudEvent != null)
                {
                    deadletterPrefix += string.Format(" for event {EventId} of type {EventType}.", args.Message.MessageId, cloudEvent.Id, cloudEvent.Type);
                }
                _logger.LogError("{Prefix}: {ReasonCode} - {Description}", deadletterPrefix, stepResult.ReasonCode, stepResult.Description);
                await args.DeadLetterMessageAsync(args.Message, stepResult.ReasonCode, stepResult.Description);
                break;
            case MessageStepStatus.Skip:

                string skipPrefix = "Skipping message {MessageId}";
                if (cloudEvent != null)
                {
                    skipPrefix += string.Format(" for event {EventId} of type {EventType}.", args.Message.MessageId, cloudEvent.Id, cloudEvent.Type);
                }
                _logger.LogWarning("{Prefix}: {ReasonCode} - {Description}", skipPrefix, stepResult.ReasonCode, stepResult.Description);
                await args.CompleteMessageAsync(args.Message);
                break;
        }
    }
    private MessageStepResult<IIntegrationEvent> ParseIntegrationEvent(CloudEvent cloudEvent)
    {
        var eventType = _integrationEventTypeRegistry.GetTypeByName(cloudEvent.Type);

        if (eventType == null)
        {
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
                return MessageStepResult<IIntegrationEvent>.DeadLetter("DeserializationResultedInNull",
                    string.Format("Failed to deserialize CloudEvent data to type {EventType}.", eventType.Name));
            }
            return MessageStepResult<IIntegrationEvent>.Success(integrationEvent);
        }
        catch (JsonException jsonEx)
        {
            return MessageStepResult<IIntegrationEvent>.DeadLetter(
                "InvalidJsonFormat",
                $"JSON Deserialization failed for integration event type {cloudEvent.Type} event {cloudEvent.Id}. ExceptionMessage: {jsonEx.Message}"
                );
        }
    }

    private async Task<MessageStepResult<bool>> DispatchEventToEventHandlers(IIntegrationEvent integrationEvent)
    {
        var eventType = integrationEvent.GetType();

        using var scope = _scopeFactory.CreateAsyncScope();

        var handlers = scope.ServiceProvider
                    .GetServices(typeof(IIntegrationEventHandler<>).MakeGenericType(eventType))
                    .Cast<IIntegrationEventHandler>();

        if (!handlers.Any())
        {
            return MessageStepResult<bool>.Skip(
                "NoRegisteredHandlers",
                $"No handlers registered for event type {eventType}.");
        }

        foreach (var handler in handlers)
        {
            if (handler == null) continue;

            await handler.HandleAsync(integrationEvent, CancellationToken.None);
        }

        return MessageStepResult<bool>.Success(true);
    }



    private MessageStepResult<CloudEvent> ParseCloudEvent(BinaryData data)
    {
        var cloudEvent = CloudEvent.Parse(data);

        if (cloudEvent?.Data is null || string.IsNullOrEmpty(cloudEvent?.Type))
        {
            return MessageStepResult<CloudEvent>.DeadLetter("InvalidCloudEvent", "Received message is not a valid CloudEvent with a Type and Data.");
        }
        return MessageStepResult<CloudEvent>.Success(cloudEvent);
    }

}
