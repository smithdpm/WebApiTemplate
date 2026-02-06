namespace Cqrs.AzureServiceBus.Reciever;
internal enum MessageStepStatus
{
    Success,
    DeadLetter,
    Skip
}
