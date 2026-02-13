namespace Cqrs.Outbox;

public class OutboxConfigurationSettings
{
    public int MaxProcessingAttempts { get; set; } = 3;
    public int BatchSize { get; set; } = 10;
    public int LockDurationInSeconds { get; set; } = 60;
    public string DefaultTopicName { get; set; } = string.Empty;
}
