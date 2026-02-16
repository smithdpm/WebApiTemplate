namespace Cqrs.Outbox;

public interface IOutboxRepository
{
    public Task<List<OutboxMessage>> FetchOutboxMessagesForProcessing(int batchSize, int lockDuration, CancellationToken cancellationToken);
    Task MarkMessageAsErrored(int id, string v, CancellationToken stoppingToken);
    Task MarkMessageAsCompleted(int id, CancellationToken stoppingToken);
    Task MarkMessageForRetry(int id, CancellationToken cancellationToken);
}
