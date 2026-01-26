namespace Cqrs.AzureServiceBus.Subscriber;
internal record MessageStepResult<T>
    (
    T Value,
    MessageStepStatus Status,
    string ReasonCode = "",
    string Description = ""
    )
{
    public bool IsSuccess => Status == MessageStepStatus.Success;

    public static MessageStepResult<T> Success(T value)
        => new(value, MessageStepStatus.Success);
    public static MessageStepResult<T> DeadLetter(string reasonCode, string description)
        => new(default!, MessageStepStatus.DeadLetter, reasonCode, description);
    public static MessageStepResult<T> Skip(string reasonCode, string description)
        => new(default!, MessageStepStatus.Skip, reasonCode, description);
}
