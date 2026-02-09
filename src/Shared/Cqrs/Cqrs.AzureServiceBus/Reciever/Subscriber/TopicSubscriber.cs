using Azure.Messaging.ServiceBus;
using Cqrs.Decorators.Registries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Cqrs.AzureServiceBus.Reciever.Subscriber;

public class TopicSubscriber : ServiceBusReciever
{
    private readonly TopicSubscriberSettings _subscriptionSettings;

    public TopicSubscriber(ILogger<TopicSubscriber> logger,
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory,
        IEventTypeRegistry integrationEventTypeRegistry,
        TopicSubscriberSettings topicSubscriberSettings)
        : base(logger, CreateProcesser(serviceBusClient, topicSubscriberSettings), scopeFactory, integrationEventTypeRegistry)
    {
        _subscriptionSettings = topicSubscriberSettings;
    }

    private static ServiceBusProcessor CreateProcesser(ServiceBusClient serviceBusClient, TopicSubscriberSettings topicSubscriberSettings)
    {
        return serviceBusClient.CreateProcessor(topicSubscriberSettings.TopicName,
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

    private async Task HandleErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            $"An error occured while running the Service Bus Topic Subscriber: TopicName ({_subscriptionSettings.TopicName}), SubscriptionName ({_subscriptionSettings.SubscriptionName})");
    }

}
