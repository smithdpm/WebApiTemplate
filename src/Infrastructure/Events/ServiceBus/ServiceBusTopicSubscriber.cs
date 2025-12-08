

using Application.Cars.IntegrationEvents;
using Azure.Messaging.ServiceBus;
using Azure.Core;
//using CloudNative.CloudEvents;
//using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Events.DomainEvents;
using SharedKernel.Events.IntegrationEvents;
using System.Text.Json;
using System.Threading;
using Azure.Messaging;
using Microsoft.Azure.Amqp.Framing;
using Application.Cars.CarBought;

namespace Infrastructure.Events.ServiceBus;
public class ServiceBusTopicSubscriber : BackgroundService
{
    private readonly ILogger<ServiceBusTopicSubscriber> _logger;
    private readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _topicName;
    private readonly string _subscriptionName;

    public ServiceBusTopicSubscriber(ILogger<ServiceBusTopicSubscriber> logger
        , ServiceBusClient serviceBusClient
        , IServiceScopeFactory scopeFactory
        , ServiceBusTopicSubscriberSettings serviceBusTopicSubscriberSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _topicName = serviceBusTopicSubscriberSettings.TopicName;
        _subscriptionName = serviceBusTopicSubscriberSettings.SubscriptionName;
        _processor = serviceBusClient.CreateProcessor(_topicName, _subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = serviceBusTopicSubscriberSettings.AutoCompleteMessages,
            MaxConcurrentCalls = serviceBusTopicSubscriberSettings.MaxConcurrentCalls
        });
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Starting Service Bus Topic Subscriber: TopicName ({_topicName}), SubscriptionName ({_subscriptionName})");

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation($"Stopping Service Bus Topic Subscriber: TopicName ({_topicName}), SubscriptionName ({_subscriptionName})");
        
        await _processor.StopProcessingAsync(stoppingToken);
    }
    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        _logger.LogInformation($"Received message: {body}");

        CloudEvent? cloudEvent = CloudEvent.Parse(args.Message.Body);

        if (cloudEvent == null)
            throw new Exception("Null message body recieved. Message must have a valid message body.");

        using var scope = _scopeFactory.CreateAsyncScope();
        try
        {
            if (cloudEvent.Data == null)
                throw new Exception("CloudEvent Data is null. Ensure that this has the Integration Event correctly stored.");

            switch (cloudEvent.Type)
            {
                case nameof(CarBoughtIntegrationEvent):
                    var integrationEvent = cloudEvent.Data.ToObjectFromJson<CarBoughtIntegrationEvent>();
                    var handlerType = typeof(IIntegrationEventHandler<CarBoughtIntegrationEvent>);
                    var handlers = scope.ServiceProvider.GetServices(handlerType);
                    foreach (var handler in handlers)
                    {
                        if (handler == null) continue;

                        var handlerWrapper = HandlerWrapper.Create(handler, integrationEvent.GetType());

                        await handlerWrapper.Handle(integrationEvent, CancellationToken.None);
                    }
                    break;
            }
            await args.CompleteMessageAsync(args.Message);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, $"Failed to process CloudEvent. Message will be retried.");
        }
    }

    private abstract class HandlerWrapper
    {
        public abstract Task Handle(IIntegrationEvent integrationEvent, CancellationToken cancellationToken);

        public static HandlerWrapper Create(object handler, Type integrationEventType)
        {
            Type wrapperType = typeof(HandlerWrapper<>).MakeGenericType(integrationEventType);
            return (HandlerWrapper)Activator.CreateInstance(wrapperType, handler);
        }
    }

    private class HandlerWrapper<TEvent>(IIntegrationEventHandler<TEvent> handler) : HandlerWrapper where TEvent : IIntegrationEvent
    {
        public override Task Handle(IIntegrationEvent domainEvent, CancellationToken cancellationToken)
        {
            return handler.HandleAsync((TEvent)domainEvent, cancellationToken);
        }
    }
    private async Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, $"An error occured while running the Service Bus Topic Subscriber: TopicName ({_topicName}), SubscriptionName ({_subscriptionName})");
    }


}
