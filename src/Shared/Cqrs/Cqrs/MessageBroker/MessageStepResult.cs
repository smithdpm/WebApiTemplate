namespace Cqrs.MessageBroker;

public record MessageStepResult<T>
    (
    T Value,
    MessageResultStatus Status,
    string ReasonCode = "",
    string Description = ""
    )
{
    public bool IsSuccess => Status == MessageResultStatus.Success;

    public static MessageStepResult<T> Success(T value)
        => new(value, MessageResultStatus.Success);
    public static MessageStepResult<T> DeadLetter(string reasonCode, string description)
        => new(default!, MessageResultStatus.DeadLetter, reasonCode, description);
    public static MessageStepResult<T> Skip(string reasonCode, string description)
        => new(default!, MessageResultStatus.Skip, reasonCode, description);

    public MessageResult ToMessageResult()
        => new MessageResult(this.Status, this.ReasonCode, this.Description);
}
