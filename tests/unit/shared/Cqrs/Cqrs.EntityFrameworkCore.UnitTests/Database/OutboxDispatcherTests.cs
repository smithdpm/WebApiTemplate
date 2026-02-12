using Cqrs.Abstractions.Events;
using Cqrs.Decorators.Registries;
using Cqrs.Events.IntegrationEvents;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using SharedKernel.Events;
using System.Text.Json;
using Cqrs.Outbox;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Testing;

namespace Cqrs.EntityFrameworkCore.UnitTests.Database;

public class OutboxDispatcherTests
{
    private readonly ILogger<OutboxDispatcher> _fakeLogger;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEventTypeRegistry _eventTypeRegistry;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IIntegrationEventDispatcher _integrationEventDispatcher;
    private readonly OutboxDispatcher _dispatcher;

    public OutboxDispatcherTests()
    {
        _fakeLogger = new FakeLogger<OutboxDispatcher>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _eventTypeRegistry = Substitute.For<IEventTypeRegistry>();
        _domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();
        _integrationEventDispatcher = Substitute.For<IIntegrationEventDispatcher>();
        var options = Options.Create(new OutboxConfigurationSettings());
        _dispatcher = new OutboxDispatcher(
            _fakeLogger,
            _outboxRepository,
            _eventTypeRegistry,
            _domainEventDispatcher,
            _integrationEventDispatcher,
            options);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessMessages_WhenMessagesAvailable()
    {
        // Arrange
        var testEvent = new TestDomainEvent { EntityId = Guid.NewGuid(), Name = "Test" };
        var message = CreateOutboxMessage(testEvent, null);
        
        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>()); // Return messages once, then empty
        
        _eventTypeRegistry.GetTypeByName(nameof(TestDomainEvent)).Returns(typeof(TestDomainEvent));
        
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received().FetchOutboxMessagesForProcessing(
            Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _domainEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IDomainEvent>>(), Arg.Any<CancellationToken>());
        await _outboxRepository.Received(1).MarkMessageAsCompleted(message.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToCorrectDispatcher_WhenDestinationNull()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { EntityId = Guid.NewGuid(), Name = "Domain Event" };
        var message = CreateOutboxMessage(domainEvent, null);
        
        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        _eventTypeRegistry.GetTypeByName(nameof(TestDomainEvent)).Returns(typeof(TestDomainEvent));

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _domainEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IDomainEvent>>(), Arg.Any<CancellationToken>());
        await _integrationEventDispatcher.DidNotReceive().DispatchEventsAsync(
            Arg.Any<List<IntegrationEventBase>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRouteToCorrectDispatcher_WhenDestinationNotNull()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { AggregateRootId = Guid.NewGuid(), Data = "Integration Event" };
        var destination = "test-topic";
        var message = CreateOutboxMessage(integrationEvent, destination);
        
        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        _eventTypeRegistry.GetTypeByName(nameof(TestIntegrationEvent)).Returns(typeof(TestIntegrationEvent));

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _integrationEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IntegrationEventBase>>(), destination, Arg.Any<CancellationToken>());
        await _domainEventDispatcher.DidNotReceive().DispatchEventsAsync(
            Arg.Any<List<IDomainEvent>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkAsErrored_WhenMaxAttemptsReached()
    {
        // Arrange
        var message = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow) { Id = 1 };
        // Set ProcessingAttempts using reflection since it has internal setter
        typeof(OutboxMessage).GetProperty("ProcessingAttempts")!.SetValue(message, 2);
        
        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        _eventTypeRegistry.GetTypeByName("TestEvent").Returns(typeof(TestDomainEvent));
        _domainEventDispatcher.DispatchEventsAsync(Arg.Any<List<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Dispatch failed"));

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageAsErrored(
            message.Id, "Dispatch failed", Arg.Any<CancellationToken>());
        await _outboxRepository.DidNotReceive().MarkMessageForRetry(
            Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkForRetry_WhenAttemptsNotExceeded()
    {
        // Arrange
        var message = new OutboxMessage("TestEvent", "{}", DateTimeOffset.UtcNow) 
        { 
            Id = 1 
        };
        // Set ProcessingAttempts using reflection since it has internal setter
        typeof(OutboxMessage).GetProperty("ProcessingAttempts")!.SetValue(message, 1);
        
        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        _eventTypeRegistry.GetTypeByName("TestEvent").Returns(typeof(TestDomainEvent));
        _domainEventDispatcher.DispatchEventsAsync(Arg.Any<List<IDomainEvent>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Transient error"));

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageForRetry(
            message.Id, Arg.Any<CancellationToken>());
        await _outboxRepository.DidNotReceive().MarkMessageAsErrored(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldContinueProcessing_WhenSingleMessageFails()
    {
        // Arrange
        var failingEvent = new TestDomainEvent { EntityId = Guid.NewGuid(), Name = "Failing Event" };
        var successEvent = new TestDomainEvent { EntityId = Guid.NewGuid(), Name = "Success Event" };
        var failingMessage = CreateOutboxMessage(failingEvent, null);
        var successMessage = CreateOutboxMessage(successEvent, null);


        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { failingMessage, successMessage }, new List<OutboxMessage>());
        
        _eventTypeRegistry.GetTypeByName("TestDomainEvent").Returns(typeof(TestDomainEvent));
        
        _domainEventDispatcher.DispatchEventsAsync(
                Arg.Is<List<IDomainEvent>>(events => events.Any()), 
                Arg.Any<CancellationToken>())
            .Returns(callInfo => 
            {
                var domainEvents = callInfo.ArgAt<List<IDomainEvent>>(0);
                var testEvent = domainEvents.OfType<TestDomainEvent>().FirstOrDefault();
                // Fail for first message, succeed for second
                if (domainEvents.Count == 1 && testEvent!.EntityId == failingEvent.EntityId)
                    throw new Exception("First message failed");
                return Task.CompletedTask;
            });

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageForRetry(failingMessage.Id, Arg.Any<CancellationToken>());
        await _outboxRepository.Received(1).MarkMessageAsCompleted(successMessage.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkCompleted_WhenValidEventProcessed()
    {
        // Arrange
        var testEvent = new TestDomainEvent { Id = Guid.NewGuid(), Name = "Valid Event" };
        var message = CreateOutboxMessage(testEvent, null);
        
        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        _eventTypeRegistry.GetTypeByName(nameof(TestDomainEvent)).Returns(typeof(TestDomainEvent));

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageAsCompleted(message.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkErrored_WhenEventTypeNotRegistered()
    {
        // Arrange
        var message = new OutboxMessage("UnregisteredEvent", "{}", DateTimeOffset.UtcNow) { Id = 1 };
        
        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        _eventTypeRegistry.GetTypeByName("UnregisteredEvent").Returns((Type)null);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageAsErrored(
            message.Id, 
            Arg.Is<string>(s => s.Contains("not a registered")), 
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkErrored_WhenJsonDeserializationFails()
    {
        // Arrange
        var message = new OutboxMessage(nameof(TestDomainEvent), "invalid json {[}", DateTimeOffset.UtcNow) { Id = 1 };
        
        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        _eventTypeRegistry.GetTypeByName(nameof(TestDomainEvent)).Returns(typeof(TestDomainEvent));

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageAsErrored(
            message.Id, 
            Arg.Is<string>(s => s.Contains("JSON Deserialization failed")), 
            Arg.Any<CancellationToken>());
    }

    private static OutboxMessage CreateOutboxMessage(object eventObj, string? destination)
    {
        var json = JsonSerializer.Serialize(eventObj);
        return new OutboxMessage(
            eventObj.GetType().Name,
            json,
            DateTimeOffset.UtcNow,
            destination)
        {
            Id = Random.Shared.Next(1, 1000)
        };
    }

    private record TestDomainEvent : DomainEventBase, IDomainEvent
    {
        public Guid EntityId { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    private record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid AggregateRootId { get; init; }
        public string Data { get; init; } = string.Empty;
    }
}