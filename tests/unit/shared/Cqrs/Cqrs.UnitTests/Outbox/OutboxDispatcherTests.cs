using System.Text.Json;
using Cqrs.Decorators.Registries;
using Cqrs.Events.DomainEvents;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using NSubstitute;
using SharedKernel.Events;
using Shouldly;

namespace Cqrs.UnitTests.Outbox;

public class OutboxDispatcherTests
{
    private readonly FakeLogger<OutboxDispatcher> _fakeLogger;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IEventTypeRegistry _eventTypeRegistry;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IIntegrationEventDispatcher _integrationEventDispatcher;
    private readonly IOptions<OutboxConfigurationSettings> _options;
    private readonly OutboxDispatcher _dispatcher;
    private readonly OutboxConfigurationSettings _settings;

    public OutboxDispatcherTests()
    {
        _fakeLogger = new FakeLogger<OutboxDispatcher>();
        _outboxRepository = Substitute.For<IOutboxRepository>();
        _eventTypeRegistry = Substitute.For<IEventTypeRegistry>();
        _domainEventDispatcher = Substitute.For<IDomainEventDispatcher>();
        _integrationEventDispatcher = Substitute.For<IIntegrationEventDispatcher>();
        
        _settings = new OutboxConfigurationSettings
        {
            BatchSize = 5,
            LockDurationInSeconds = 30,
            MaxProcessingAttempts = 3,
            DefaultTopicName = "default-topic"
        };
        _options = Options.Create(_settings);
        
        _dispatcher = new OutboxDispatcher(
            _fakeLogger,
            _outboxRepository,
            _eventTypeRegistry,
            _domainEventDispatcher,
            _integrationEventDispatcher,
            _options);

        _eventTypeRegistry.GetTypeByName(nameof(TestDomainEvent))
            .Returns(typeof(TestDomainEvent));

        _eventTypeRegistry.GetTypeByName(nameof(TestIntegrationEvent))
            .Returns(typeof(TestIntegrationEvent));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessDomainEvent_WhenDestinationIsNull()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var message = CreateOutboxMessage(domainEvent, id: 1,processingAttempts: 0);

        var cancellationToken = TestContext.Current.CancellationToken;

        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _domainEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Is<List<IDomainEvent>>(events => events.Count == 1),
            Arg.Any<CancellationToken>());
        await _outboxRepository.Received(1).MarkMessageAsCompleted(message.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessIntegrationEvent_WhenDestinationIsNotNull()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var destination = "custom-topic";
        var message = CreateOutboxMessage(integrationEvent, id: 2, processingAttempts: 0, destination: destination);
        
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _integrationEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Is<List<IntegrationEventBase>>(events => events.Count == 1),
            destination,
            Arg.Any<CancellationToken>());
        await _outboxRepository.Received(1).MarkMessageAsCompleted(message.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkAsErrored_WhenEventTypeNotRegistered()
    {
        // Arrange
        var message = new OutboxMessage(
            "UnregisteredEvent",
            "{}",
            DateTimeOffset.UtcNow,
            null)
        {
            Id = 3,
            ProcessingAttempts = 0
        };
        
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        _eventTypeRegistry.GetTypeByName("UnregisteredEvent")
            .Returns((Type?)null);
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageAsErrored(
            message.Id,
            Arg.Is<string>(s => s.Contains("not a registered")),
            Arg.Any<CancellationToken>());
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkAsErrored_WhenJsonDeserializationFails()
    {
        // Arrange
        var message = new OutboxMessage(
            nameof(TestDomainEvent),
            "invalid json",
            DateTimeOffset.UtcNow,
            null)
        {
            Id = 4,
            ProcessingAttempts = 0
        };
        
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageAsErrored(
            message.Id,
            Arg.Is<string>(s => s.Contains("JSON Deserialization failed")),
            Arg.Any<CancellationToken>());
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Level == LogLevel.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRetryMessage_WhenProcessingFailsAndBelowMaxAttempts()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var message = CreateOutboxMessage(domainEvent, id: 5, processingAttempts: 1);
        
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());

        _domainEventDispatcher.DispatchEventsAsync(
            Arg.Any<List<IDomainEvent>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("Processing failed")));
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageForRetry(message.Id, Arg.Any<CancellationToken>());
        await _outboxRepository.DidNotReceive().MarkMessageAsErrored(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Level == LogLevel.Error);
        logs.ShouldNotContain(log => log.Message.Contains("exceeded max processing attempts"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkAsErrored_WhenProcessingFailsAndExceedsMaxAttemptsFromSettings()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var message = CreateOutboxMessage(domainEvent, id: 6, processingAttempts: _settings.MaxProcessingAttempts - 1);

        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());

        
        var exception = new Exception("Processing failed");
        _domainEventDispatcher.DispatchEventsAsync(
            Arg.Any<List<IDomainEvent>>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromException(exception));
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageAsErrored(message.Id, exception.Message, Arg.Any<CancellationToken>());
        await _outboxRepository.DidNotReceive().MarkMessageForRetry(Arg.Any<int>(), Arg.Any<CancellationToken>());
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Level == LogLevel.Error && log.Message.Contains("exceeded max processing attempts"));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseBatchSizeFromSettings_WhenFetchingMessages()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), cancellationToken)
            .Returns(new List<OutboxMessage>());
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received().FetchOutboxMessagesForProcessing(
            _settings.BatchSize,
            _settings.LockDurationInSeconds,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseLockDurationFromSettings_WhenFetchingMessages()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), cancellationToken)
            .Returns(new List<OutboxMessage>());
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received().FetchOutboxMessagesForProcessing(
            Arg.Any<int>(),
            _settings.LockDurationInSeconds,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldProcessMultipleMessages_InSingleBatch()
    {
        // Arrange
        var messages = new List<OutboxMessage>
        {
            CreateOutboxMessage(new TestDomainEvent { Id = Guid.NewGuid() }, id: 7, processingAttempts:0),
            CreateOutboxMessage(new TestIntegrationEvent { EventId = Guid.NewGuid() }, id: 8, processingAttempts: 0)
        };
        
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(messages, new List<OutboxMessage>());
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _domainEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IDomainEvent>>(),
            Arg.Any<CancellationToken>());
        await _integrationEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IntegrationEventBase>>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await _outboxRepository.Received(1).MarkMessageAsCompleted(7, Arg.Any<CancellationToken>());
        await _outboxRepository.Received(1).MarkMessageAsCompleted(8, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseDefaultDestinationFromSettings_WhenIntegrationEventDestinationIsDefault()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var message = CreateOutboxMessage(integrationEvent, id: 9, processingAttempts: 0);
        
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _integrationEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IntegrationEventBase>>(),
            _settings.DefaultTopicName,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseDefaultDestinationFromSettings_WhenIntegrationEventDestinationIsEmpty()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var message = CreateOutboxMessage(integrationEvent, id: 11, processingAttempts: 0, destination: string.Empty);

        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _integrationEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IntegrationEventBase>>(),
            _settings.DefaultTopicName,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseDefaultDestinationFromSettings_WhenIntegrationEventDestinationIsWhitespace()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var message = CreateOutboxMessage(integrationEvent, id: 12, processingAttempts: 0, destination: "   "); 

        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _integrationEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IntegrationEventBase>>(),
            _settings.DefaultTopicName,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("DEFAULT")]
    [InlineData("DeFaUlT")]
    [InlineData("default")]
    public async Task ExecuteAsync_ShouldUseDefaultDestinationFromSettings_WhenIntegrationEventDestinationIsDefaultCaseInsensitive(string destinationCase)
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };

        var message = CreateOutboxMessage(integrationEvent, id: 13, processingAttempts: 0, destination: destinationCase); 

        var cancellationToken = TestContext.Current.CancellationToken;

        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());

        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _integrationEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IntegrationEventBase>>(),
            _settings.DefaultTopicName,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepCustomDestination_WhenIntegrationEventDestinationIsNotDefault()
    {
        // Arrange
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid() };
        var customDestination = "custom-topic";
        var message = CreateOutboxMessage(integrationEvent, id: 14, processingAttempts: 0, destination: customDestination); 
        
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { message }, new List<OutboxMessage>());
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _integrationEventDispatcher.Received(1).DispatchEventsAsync(
            Arg.Any<List<IntegrationEventBase>>(),
            customDestination,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldContinueProcessing_WhenSingleMessageFails()
    {
        // Arrange
        var failingEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        var successEvent = new TestDomainEvent { Id = Guid.NewGuid() };
        
        var failingMessage = CreateOutboxMessage(failingEvent, id: 15, processingAttempts: 0);
        var successMessage = CreateOutboxMessage(successEvent, id: 16, processingAttempts: 0);
        
        var cancellationToken = TestContext.Current.CancellationToken;
        
        _outboxRepository.FetchOutboxMessagesForProcessing(_settings.BatchSize, _settings.LockDurationInSeconds, cancellationToken)
            .Returns(new List<OutboxMessage> { failingMessage, successMessage });

        _domainEventDispatcher.DispatchEventsAsync(
                Arg.Is<List<IDomainEvent>>(events => events.Any()), 
                Arg.Any<CancellationToken>())
            .Returns(callInfo => 
            {
                var domainEvents = callInfo.ArgAt<List<IDomainEvent>>(0);
                var testEvent = domainEvents.OfType<TestDomainEvent>().FirstOrDefault();
                // Fail for first message, succeed for second
                if (domainEvents.Count == 1 && testEvent?.Id == failingEvent.Id)
                    throw new Exception("First message failed");
                return Task.CompletedTask;
            });
        
        // Act
        await _dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await _outboxRepository.Received(1).MarkMessageForRetry(failingMessage.Id, Arg.Any<CancellationToken>());
        await _outboxRepository.Received(1).MarkMessageAsCompleted(successMessage.Id, Arg.Any<CancellationToken>());     
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseCustomConfigurationSettings_WhenProvided()
    {
        // Arrange
        var customSettings = new OutboxConfigurationSettings
        {
            BatchSize = 20,
            LockDurationInSeconds = 120,
            MaxProcessingAttempts = 5,
            DefaultTopicName = "custom-default-topic"
        };
        
        var fakeLogger = new FakeLogger<OutboxDispatcher>();
        var outboxRepository = Substitute.For<IOutboxRepository>();
        
        var dispatcher = new OutboxDispatcher(
            fakeLogger,
            outboxRepository,
            Substitute.For<IEventTypeRegistry>(),
            Substitute.For<IDomainEventDispatcher>(),
            Substitute.For<IIntegrationEventDispatcher>(),
            Options.Create(customSettings));

        var cancellationToken = TestContext.Current.CancellationToken;
        
        outboxRepository.FetchOutboxMessagesForProcessing(Arg.Any<int>(), Arg.Any<int>(), cancellationToken)
            .Returns(new List<OutboxMessage>());
        
        // Act
        await dispatcher.ExecuteAsync(cancellationToken);

        // Assert
        await outboxRepository.Received().FetchOutboxMessagesForProcessing(
            customSettings.BatchSize,
            customSettings.LockDurationInSeconds,
            Arg.Any<CancellationToken>());
    }
    private static OutboxMessage CreateOutboxMessage(TestDomainEvent domainEvent, int id, int processingAttempts = 0)
    {
        return new OutboxMessage(
                        nameof(TestDomainEvent),
                        JsonSerializer.Serialize(domainEvent),
                        DateTimeOffset.UtcNow,
                        null)
        {
            Id = id,
            ProcessingAttempts = processingAttempts
        };
    }

    private static OutboxMessage CreateOutboxMessage(TestIntegrationEvent integrationEvent, 
        int id, 
        int processingAttempts = 0,
        string destination = "default")
    {
        return new OutboxMessage(
                        nameof(TestIntegrationEvent),
                        JsonSerializer.Serialize(integrationEvent),
                        DateTimeOffset.UtcNow,
                        destination)
        {
            Id = id,
            ProcessingAttempts = processingAttempts
        };
    }

    public class TestDomainEvent : IDomainEvent
    {
        public Guid Id { get; set; }
    }

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
    }
}