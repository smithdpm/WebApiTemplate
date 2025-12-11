
namespace Infrastructure.Events.ServiceBus;

public class ServiceBusTopicSubscriberSettings
{
    public required string TopicName { get; set; }
    public required string SubscriptionName { get; set; }

    public bool AutoCompleteMessages { get; set; } = false;
    public int MaxConcurrentCalls { get; set; } = 1;
}