

using Application.Cars.IntegrationEvents;
using Azure.Messaging.ServiceBus;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.Events.DomainEvents;
using SharedKernel.Events.IntegrationEvents;
using System.Text.Json;
using System.Threading;

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
        , string topicName
        , string subscriptionName)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _processor = serviceBusClient.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(string.Format(
            "Starting Service Bus Topic Subscriber: TopicName ({0}), SubscriptionName ({1})"
            , _topicName, _subscriptionName));

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += HandleErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation(string.Format(
            "Stopping Service Bus Topic Subscriber: TopicName ({0}), SubscriptionName ({1})"
            , _topicName, _subscriptionName));
        
        await _processor.StopProcessingAsync(stoppingToken);
    }
    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        string body = args.Message.Body.ToString();
        _logger.LogInformation($"Received message: {body}");

        using var scope = _scopeFactory.CreateAsyncScope();
        try
        {
            var jsonFormatter = new JsonEventFormatter();
            CloudEvent cloudEvent = await jsonFormatter.DecodeStructuredModeMessageAsync(args.Message.Body.ToStream(), null, null);


            switch (cloudEvent.Type)
            {
                case "Application.Cars.IntegrationEvents.CarSoldIntegrationEvent":
                    var integrationEvent = JsonSerializer.Deserialize<CarSoldIntegrationEvent>(body);
                    var handlerType = typeof(IIntegrationEventHandler<>).MakeGenericType(integrationEvent.GetType());
                    var handlers = scope.ServiceProvider.GetServices(handlerType);
                    foreach (var handler in handlers)
                    {
                        if (handler == null) continue;

                        var handlerWrapper = HandlerWrapper.Create(handler, integrationEvent.GetType());

                        await handlerWrapper.Handle(integrationEvent, CancellationToken.None);
                    }
                    break;
            }
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
        throw new NotImplementedException();
    }


}
