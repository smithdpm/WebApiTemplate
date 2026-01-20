namespace Cqrs.AzureServiceBus.Subscriber;
internal enum MessageStepStatus
{
    Success,
    DeadLetter,
    Skip
}
