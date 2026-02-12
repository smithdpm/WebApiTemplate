using Azure.Messaging.ServiceBus;
using Cqrs.MessageBroker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace Cqrs.AzureServiceBus.Reciever.Subscriber;

public class ServiceBusWorker : BackgroundService
{
    protected readonly ILogger<ServiceBusWorker> _logger;
    protected readonly ServiceBusProcessor _processor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TopicSubscriberSettings _subscriptionSettings;

    public ServiceBusWorker(ILogger<ServiceBusWorker> logger,
        ServiceBusClient serviceBusClient,
        IServiceScopeFactory scopeFactory,
        TopicSubscriberSettings topicSubscriberSettings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _processor = serviceBusClient.CreateProcessor(topicSubscriberSettings.TopicName,
            topicSubscriberSettings.SubscriptionName,
            new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = topicSubscriberSettings.MaxConcurrentCalls
            });
        _subscriptionSettings = topicSubscriberSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Starting Service Bus Topic Subscriber: TopicName ({_subscriptionSettings.TopicName}), SubscriptionName ({_subscriptionSettings.SubscriptionName})");

        _processor.ProcessMessageAsync += OnProcessMessageAsync;
        _processor.ProcessErrorAsync += OnProcessErrorAsync;

        await _processor.StartProcessingAsync(stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnProcessMessageAsync(ProcessMessageEventArgs args)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler>();


        var messageResult = await handler.HandleMessageAsync(args.Message.MessageId, args.Message.Body, args.CancellationToken);

        await FinalizeMessageProcessingAsync(args, messageResult, args.CancellationToken);
    }

    private Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception,
            $"An error occured while running the Service Bus Topic Subscriber: TopicName ({_subscriptionSettings.TopicName}), SubscriptionName ({_subscriptionSettings.SubscriptionName})");

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            $"Stopping Service Bus Topic Subscriber: TopicName ({_subscriptionSettings.TopicName}), SubscriptionName ({_subscriptionSettings.SubscriptionName})");

        if (_processor != null) await _processor.DisposeAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task FinalizeMessageProcessingAsync(ProcessMessageEventArgs args, MessageResult messageResult, CancellationToken cancellationToken)
    {
        switch (messageResult.Status)
        {
            case MessageResultStatus.Success:
                await args.CompleteMessageAsync(args.Message, cancellationToken);
                break;
            case MessageResultStatus.DeadLetter:
                await args.DeadLetterMessageAsync(args.Message, messageResult.ReasonCode, messageResult.Description, cancellationToken);
                break;
            case MessageResultStatus.Skip:
                await args.CompleteMessageAsync(args.Message, cancellationToken);
                break;
        }
    }
}
