namespace Cqrs.Events.IntegrationEvents.MessageHandling;

public enum MessageResultStatus
{
    Success,
    DeadLetter,
    Skip
}
