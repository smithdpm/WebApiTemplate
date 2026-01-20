
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Database;
internal class OutboxRepository<TDbContext> : IOutboxRepository where TDbContext : DbContext
{
    protected readonly IDbContextFactory<TDbContext> _contextFactory;

    public OutboxRepository(IDbContextFactory<TDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    public async Task<List<OutboxMessage>> FetchOutboxMessagesForProcessing(int batchSize, int lockDuration, CancellationToken cancellationToken)
    {
        using var contextForStrategy = _contextFactory.CreateDbContext();
        var strategy = contextForStrategy.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var context = _contextFactory.CreateDbContext();
            using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            var messages = await context.Set<OutboxMessage>()
                .FromSql($@"
                SELECT TOP ({batchSize}) * 
                FROM outbox.OutboxMessages WITH (UPDLOCK, READPAST)
                WHERE ProcessedAtUtc IS NULL 
                    AND (LockedUntilUtc IS NULL OR LockedUntilUtc < GETUTCDATE())
                ORDER BY Id ASC")
                .ToListAsync(cancellationToken);

            if (messages.Any())
            {
                var lockExpiry = DateTimeOffset.UtcNow.AddSeconds(lockDuration);
                foreach (var message in messages)
                {
                    message.LockedUntilUtc = lockExpiry;
                }

                await context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return messages;
        });

    }

    public async Task MarkMessageAsErrored(int id, string errorMessage, CancellationToken cancellationToken)
    {
        await _contextFactory.CreateDbContext().Set<OutboxMessage>().Where(m => m.Id == id)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(x=>x.Error, errorMessage)
                .SetProperty(x => x.ProcessingAttempts, x => x.ProcessingAttempts + 1)
                .SetProperty(x=>x.ProcessedAtUtc, DateTimeOffset.UtcNow), 
                cancellationToken);
    }

    public async Task MarkMessageAsCompleted(int id, CancellationToken cancellationToken)
    {
        await _contextFactory.CreateDbContext().Set<OutboxMessage>().Where(m => m.Id == id)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(x => x.ProcessedAtUtc, DateTimeOffset.UtcNow)
                .SetProperty(x => x.ProcessingAttempts, x => x.ProcessingAttempts + 1),
                cancellationToken);
    }
    public async Task MarkMessageForRetry(int id, CancellationToken cancellationToken)
    {
        await _contextFactory.CreateDbContext().Set<OutboxMessage>().Where(m => m.Id == id)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(x => x.ProcessingAttempts, x=> x.ProcessingAttempts + 1),
                cancellationToken);
    }
}
