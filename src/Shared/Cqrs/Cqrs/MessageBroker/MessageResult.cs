namespace Cqrs.MessageBroker;

public record MessageResult
    (MessageResultStatus Status,
    string ReasonCode = "",
    string Description = "");
