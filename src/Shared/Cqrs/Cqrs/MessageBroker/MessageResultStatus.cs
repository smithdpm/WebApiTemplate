namespace Cqrs.MessageBroker;

public enum MessageResultStatus
{
    Success,
    DeadLetter,
    Skip
}
