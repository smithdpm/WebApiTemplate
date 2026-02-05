using Cqrs.IntegrationTests.Fixtures;
using Cqrs.IntegrationTests.Fixtures.CollectionFixtures;
using Cqrs.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Cqrs.IntegrationTests.Infrastructure.Database;

[Collection(nameof(OutboxRepositoryCollection))]
public class OutboxRepositoryTests : IAsyncLifetime
{
    private readonly OutboxRepositoryFixture _fixture;
    private readonly IOutboxRepository _repository;

    public OutboxRepositoryTests(OutboxRepositoryFixture outboxRepositoryFixture)
    {
        _fixture = outboxRepositoryFixture;
        _repository = outboxRepositoryFixture.ServiceProvider.GetRequiredService<IOutboxRepository>();
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.CleanDatabaseAsync();
    }

    public async ValueTask DisposeAsync() => await Task.CompletedTask;

    [Fact]
    public async Task FetchOutboxMessagesForProcessing_ShouldOnlyReturnUnprocessedMessages()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        var unprocessedMessage = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow);
        var processedMessage = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow);
        var processedErroredMessage = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow);
        
        context.Set<OutboxMessage>().AddRange(unprocessedMessage, processedMessage);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Mark one message as processed and one as errored
        await _repository.MarkMessageAsCompleted(processedMessage.Id, TestContext.Current.CancellationToken);
        await _repository.MarkMessageAsErrored(processedErroredMessage.Id, "Test error", TestContext.Current.CancellationToken);

        // Act
        var messages = await _repository.FetchOutboxMessagesForProcessing(10, 60, TestContext.Current.CancellationToken);

        // Assert
        messages.Count.ShouldBe(1);
        messages[0].ProcessedAtUtc.ShouldBeNull();
    }

    [Fact]
    public async Task FetchOutboxMessagesForProcessing_ShouldNotReturnMessages_WhenLockNotExpired()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        var lockedMessage = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow)
        {
            LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        
        context.Set<OutboxMessage>().Add(lockedMessage);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var messages = await _repository.FetchOutboxMessagesForProcessing(10, 60, TestContext.Current.CancellationToken);

        // Assert
        messages.Count.ShouldBe(0);
    }

    [Fact]
    public async Task FetchOutboxMessagesForProcessing_ShouldReturnMessages_WhenLockExpired()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        var expiredLockMessage = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow)
        {
            LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        
        context.Set<OutboxMessage>().Add(expiredLockMessage);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var messages = await _repository.FetchOutboxMessagesForProcessing(10, 60, TestContext.Current.CancellationToken);

        // Assert
        messages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task FetchOutboxMessagesForProcessing_ShouldLimitBatchSize()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        // Add 10 messages
        for (int i = 0; i < 10; i++)
        {
            context.Set<OutboxMessage>().Add(new OutboxMessage($"TestEvent{i}", "{}", DateTimeOffset.UtcNow));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var messages = await _repository.FetchOutboxMessagesForProcessing(5, 60, TestContext.Current.CancellationToken);

        // Assert
        messages.Count.ShouldBe(5);
    }

    [Fact]
    public async Task FetchOutboxMessagesForProcessing_ShouldSetLockOnFetchedMessages()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        var message = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow);
        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var originalId = message.Id;

        // Act
        var messages = await _repository.FetchOutboxMessagesForProcessing(10, 60, TestContext.Current.CancellationToken);

        // Assert
        messages.Count.ShouldBe(1);
        messages[0].LockedUntilUtc.ShouldNotBeNull();
        messages[0].LockedUntilUtc!.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
        
        // Verify the lock is persisted in the database
        var dbMessage = await GetOutboxMessageByIdAsync(originalId, TestContext.Current.CancellationToken);
        dbMessage!.LockedUntilUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task FetchOutboxMessagesForProcessing_ShouldHandleEmptyQueue()
    {
        // Arrange - no messages in database

        // Act
        var messages = await _repository.FetchOutboxMessagesForProcessing(10, 60, TestContext.Current.CancellationToken);

        // Assert
        messages.ShouldNotBeNull();
        messages.Count.ShouldBe(0);
    }

    [Fact]
    public async Task FetchOutboxMessagesForProcessing_ShouldOrderById()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        // Add messages in random order
        var message3 = new OutboxMessage("TestEvent3", "{}", DateTimeOffset.UtcNow);
        var message1 = new OutboxMessage("TestEvent1", "{}", DateTimeOffset.UtcNow);
        var message2 = new OutboxMessage("TestEvent2", "{}", DateTimeOffset.UtcNow);
        
        context.Set<OutboxMessage>().AddRange(message3, message1, message2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var messages = await _repository.FetchOutboxMessagesForProcessing(10, 60, TestContext.Current.CancellationToken);

        // Assert
        messages.Count.ShouldBe(3);
        messages[0].Id.ShouldBeLessThan(messages[1].Id);
        messages[1].Id.ShouldBeLessThan(messages[2].Id);
    }

    [Fact]
    public async Task FetchOutboxMessagesForProcessing_ShouldHandleConcurrentAccess()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        for (int i = 0; i < 10; i++)
        {
            context.Set<OutboxMessage>().Add(new OutboxMessage($"TestEvent{i}", "{}", DateTimeOffset.UtcNow));
        }
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Simulate concurrent access
        var task1 = _repository.FetchOutboxMessagesForProcessing(5, 60, TestContext.Current.CancellationToken);
        var task2 = _repository.FetchOutboxMessagesForProcessing(5, 60, TestContext.Current.CancellationToken);
        
        var results = await Task.WhenAll(task1, task2);
        var messages1 = results[0];
        var messages2 = results[1];

        // Assert - No message should be in both results (no duplicates)
        var ids1 = messages1.Select(m => m.Id).ToHashSet();
        var ids2 = messages2.Select(m => m.Id).ToHashSet();
        
        ids1.Intersect(ids2).ShouldBeEmpty();
        
        // Total unique messages should be <= 10
        ids1.Union(ids2).Count().ShouldBeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task MarkMessageAsCompleted_ShouldUpdateProcessedTimestamp()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        var message = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow);
        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _repository.MarkMessageAsCompleted(message.Id, TestContext.Current.CancellationToken);

        // Assert
        var updatedMessage = await GetOutboxMessageByIdAsync(message.Id, TestContext.Current.CancellationToken);
        
        updatedMessage!.ProcessedAtUtc.ShouldNotBeNull();
        updatedMessage.ProcessedAtUtc.Value.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddSeconds(-5));
        updatedMessage.ProcessingAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task MarkMessageAsErrored_ShouldSetErrorAndTimestamp()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        var message = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow);
        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var errorMessage = "Test error occurred";

        // Act
        await _repository.MarkMessageAsErrored(message.Id, errorMessage, TestContext.Current.CancellationToken);

        // Assert
        var updatedMessage = await GetOutboxMessageByIdAsync(message.Id, TestContext.Current.CancellationToken);
        updatedMessage!.Error.ShouldBe(errorMessage);
        updatedMessage.ProcessedAtUtc.ShouldNotBeNull();
        updatedMessage.ProcessingAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task MarkMessageAsErrored_ShouldIncrementAttempts()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        var message = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow);
        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var savedMessage = await GetOutboxMessageByIdAsync(message.Id, TestContext.Current.CancellationToken); 

        // Act
        await _repository.MarkMessageAsErrored(message.Id, "Error", TestContext.Current.CancellationToken);

        // Assert
        var updatedMessage = await GetOutboxMessageByIdAsync(message.Id, TestContext.Current.CancellationToken);
        updatedMessage.ProcessingAttempts.ShouldBe(savedMessage.ProcessingAttempts + 1);
    }

    [Fact]
    public async Task MarkMessageForRetry_ShouldOnlyIncrementAttempts()
    {
        // Arrange
        using var context = await _fixture.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var message = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow);
        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var savedMessage = await GetOutboxMessageByIdAsync(message.Id, TestContext.Current.CancellationToken); 
        
        // Act
        await _repository.MarkMessageForRetry(message.Id, TestContext.Current.CancellationToken);

        // Assert
        var updatedMessage = await GetOutboxMessageByIdAsync(message.Id, TestContext.Current.CancellationToken);

        updatedMessage!.ProcessingAttempts.ShouldBe(savedMessage.ProcessingAttempts + 1);
        updatedMessage.ProcessedAtUtc.ShouldBeNull();
        updatedMessage.Error.ShouldBeNull();
    }
    
    private async Task<OutboxMessage> GetOutboxMessageByIdAsync(int id, CancellationToken cancellationToken)
    {
        using var context = await _fixture.CreateDbContextAsync(cancellationToken);
        return await context.Set<OutboxMessage>().FirstAsync(m => m.Id == id, cancellationToken);
    }
}