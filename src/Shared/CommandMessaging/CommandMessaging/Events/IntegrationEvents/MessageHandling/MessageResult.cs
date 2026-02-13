namespace Cqrs.Events.IntegrationEvents.MessageHandling;

public record MessageResult
    (MessageResultStatus Status,
    string ReasonCode = "",
    string Description = "");
