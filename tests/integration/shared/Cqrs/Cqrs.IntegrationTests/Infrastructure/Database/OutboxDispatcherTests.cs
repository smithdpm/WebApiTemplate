using Cqrs.Abstractions.Events;
using Cqrs.Decorators.Registries;
using Cqrs.Events.IntegrationEvents;
using Cqrs.IntegrationTests.TestCollections;
using Cqrs.IntegrationTests.TestCollections.Environments;
using Cqrs.Outbox;
using Microsoft.Extensions.Options;
using NSubstitute;
using Polly;
using SharedKernel.Events;
using Shouldly;

namespace Cqrs.IntegrationTests.Infrastructure.Database;

[Collection(nameof(OutboxRepositoryCollection))]
public class OutboxDispatcherTests : IAsyncLifetime
{
    private readonly OutboxRepositoryEnvironment _environment;
    private readonly IOutboxRepository _repository;
    private readonly IEventTypeRegistry _eventTypeRegistry;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IIntegrationEventDispatcher _integrationEventDispatcher;
    private readonly ILogger<OutboxDispatcher> _logger;

    private readonly OutboxDispatcher _outboxDispatcher;
    
    public OutboxDispatcherTests(OutboxRepositoryEnvironment environment)
    {
        _environment = environment;
        _repository = environment.OutboxRepository.ServiceProvider.GetRequiredService<IOutboxRepository>();
        _eventTypeRegistry = Substitute.For<IEventTypeRegistry>();
        _domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();
        _integrationEventDispatcher = Substitute.For<IIntegrationEventDispatcher>();
        _logger = Substitute.For<ILogger<OutboxDispatcher>>();
        
        _eventTypeRegistry.GetTypeByName("TestDomainEvent").Returns(typeof(TestDomainEvent));
        _eventTypeRegistry.GetTypeByName("TestIntegrationEvent").Returns(typeof(TestIntegrationEvent));
        var options = Options.Create(new OutboxConfigurationSettings());

        _outboxDispatcher = new OutboxDispatcher(
            _logger,
            _repository,
            _eventTypeRegistry,
            _domainEventDispatcher,
            _integrationEventDispatcher,
            options);
    }

    public async ValueTask InitializeAsync()
    {
        await _environment.OutboxRepository.CleanDatabaseAsync();
    }

    public async ValueTask DisposeAsync() => await Task.CompletedTask;

    [Fact]
    public async Task BackgroundService_ShouldPollContinuously()
    {
        // Arrange
        using var context = await _environment.OutboxRepository.CreateDbContextAsync(TestContext.Current.CancellationToken);

        var message1 = new OutboxMessage("TestDomainEvent", "{\"EntityId\":\"123\"}", DateTimeOffset.UtcNow);
        var message2 = new OutboxMessage("TestDomainEvent", "{\"EntityId\":\"124\"}", DateTimeOffset.UtcNow);
        context.Set<OutboxMessage>().Add(message1);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        // Act
        await _outboxDispatcher.StartAsync(cts.Token);
        await Task.Delay(500, TestContext.Current.CancellationToken); // Let it run for a bit
        context.Set<OutboxMessage>().Add(message2);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await Task.Delay(500, TestContext.Current.CancellationToken); // Let it run for a bit
        await _outboxDispatcher.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        await _domainEventDispatcher.Received(2).DispatchEventsAsync(
            Arg.Any<List<IDomainEvent>>(), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackgroundService_ShouldDispatchToBothDispatchers()
    {
        // Arrange
        using var context = await _environment.OutboxRepository.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        // Add messages with and without destinations
        var domainMessage = new OutboxMessage("TestDomainEvent", "{\"EntityId\":\"123\"}", DateTimeOffset.UtcNow);
        var integrationMessage = new OutboxMessage("TestIntegrationEvent", "{\"AggregateRootId\":\"456\"}", DateTimeOffset.UtcNow, "test-topic");
        
        context.Set<OutboxMessage>().AddRange(domainMessage, integrationMessage);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        // Act
        await _outboxDispatcher.StartAsync(cts.Token);
        await WaitForAllMessagesToBeProcessedAsync(TestContext.Current.CancellationToken);
        await _outboxDispatcher.StopAsync(TestContext.Current.CancellationToken);

        // Assert
        await _domainEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IDomainEvent>>(), 
            Arg.Any<CancellationToken>());
            
        await _integrationEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IntegrationEventBase>>(), 
            "test-topic", 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BackgroundService_ShouldHandleDispatcherExceptions()
    {
        // Arrange
        _domainEventDispatcher
            .DispatchEventsAsync(Arg.Any<List<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Test dispatcher error")));

        using var context = await _environment.OutboxRepository.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        var message = new OutboxMessage("TestDomainEvent", "{\"EntityId\":\"123\"}", DateTimeOffset.UtcNow);
        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));

        // Act
        await _outboxDispatcher.StartAsync(cts.Token);
        await WaitForMessageProcessingAttemptAsync(message.Id, 1, TestContext.Current.CancellationToken);
        await _outboxDispatcher.StopAsync(TestContext.Current.CancellationToken);

        // Assert - Message should be marked for retry
        using var verifyContext = await _environment.OutboxRepository.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var updatedMessage = await verifyContext.Set<OutboxMessage>().FindAsync(new object[] { message.Id }, TestContext.Current.CancellationToken);
        updatedMessage!.ProcessingAttempts.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task BackgroundService_ShouldRespectMaxAttempts()
    {
        // Arrange
        _domainEventDispatcher
            .DispatchEventsAsync(Arg.Any<List<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Persistent error")));

        using var context = await _environment.OutboxRepository.CreateDbContextAsync(TestContext.Current.CancellationToken);
        
        // Add message and set attempts to threshold (2, so next will be 3rd attempt)
        var message = new OutboxMessage("TestDomainEvent", "{\"EntityId\":\"123\"}", DateTimeOffset.UtcNow);
        context.Set<OutboxMessage>().Add(message);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        // Increment attempts to 2 using repository methods
        await _repository.MarkMessageForRetry(message.Id, TestContext.Current.CancellationToken);
        await _repository.MarkMessageForRetry(message.Id, TestContext.Current.CancellationToken);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        // Act
        await _outboxDispatcher.StartAsync(cts.Token);
        await WaitForMessageToBeErroredAsync(message.Id, TestContext.Current.CancellationToken);
        await _outboxDispatcher.StopAsync(TestContext.Current.CancellationToken);

        // Assert - Message should be marked as errored
        using var verifyContext = await _environment.OutboxRepository.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var updatedMessage = await verifyContext.Set<OutboxMessage>().FindAsync(new object[] { message.Id }, TestContext.Current.CancellationToken);
            
        updatedMessage!.Error.ShouldNotBeNull();
        updatedMessage.ProcessedAtUtc.ShouldNotBeNull();
        updatedMessage.ProcessingAttempts.ShouldBe(3);
    }

    private Task WaitForAllMessagesToBeProcessedAsync(CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .HandleResult<bool>(result => !result)
            .WaitAndRetryAsync(50, _ => TimeSpan.FromMilliseconds(200));

        return retryPolicy.ExecuteAsync(
            () => _environment.OutboxRepository.CheckAllMessagesHaveBeenProcessedAsync());
    }

    private Task<bool> WaitForMessageProcessingAttemptAsync(int messageId, int expectedAttempts, CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .HandleResult<bool>(result => !result)
            .WaitAndRetryAsync(25, _ => TimeSpan.FromMilliseconds(200));

        return retryPolicy.ExecuteAsync(async () =>
        {
            using var context = await _environment.OutboxRepository.CreateDbContextAsync(cancellationToken);
            var message = await context.Set<OutboxMessage>()
                .FindAsync(new object[] { messageId }, cancellationToken);
            return message?.ProcessingAttempts >= expectedAttempts;
        });
    }

    private Task<bool> WaitForMessageToBeErroredAsync(int messageId, CancellationToken cancellationToken)
    {
        var retryPolicy = Policy
            .HandleResult<bool>(result => !result)
            .WaitAndRetryAsync(25, _ => TimeSpan.FromMilliseconds(200));

        return retryPolicy.ExecuteAsync(async () =>
        {
            using var context = await _environment.OutboxRepository.CreateDbContextAsync(cancellationToken);
            var message = await context.Set<OutboxMessage>()
                .FindAsync(new object[] { messageId }, cancellationToken);
            return message?.Error != null && message.ProcessedAtUtc.HasValue;
        });
    }

    private record TestDomainEvent : DomainEventBase, IDomainEvent
    {
        public string EntityId { get; init; } = string.Empty;
    }

    private record TestIntegrationEvent : IntegrationEventBase
    {
        public string AggregateRootId { get; init; } = string.Empty;
    }
}