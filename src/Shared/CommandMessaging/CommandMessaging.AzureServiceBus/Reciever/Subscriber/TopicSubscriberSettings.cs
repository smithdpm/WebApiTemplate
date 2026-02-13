namespace Cqrs.AzureServiceBus.Reciever.Subscriber;

public class TopicSubscriberSettings
{
    public required string TopicName { get; set; }
    public required string SubscriptionName { get; set; }
    public int MaxConcurrentCalls { get; set; } = 1;
}