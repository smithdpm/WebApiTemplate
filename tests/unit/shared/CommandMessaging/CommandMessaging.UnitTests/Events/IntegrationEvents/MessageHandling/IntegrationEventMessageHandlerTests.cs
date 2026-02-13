using Azure.Messaging;
using Ardalis.Result;
using Cqrs.Decorators.Registries;
using Cqrs.Events.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using Shouldly;
using System.Text.Json;
using Cqrs.Events.IntegrationEvents.MessageHandling;

namespace Cqrs.UnitTests.Events.IntegrationEvents.MessageHandling;

public class IntegrationEventMessageHandlerTests
{
    private readonly FakeLogger<IntegreationEventMessageHandler> _fakeLogger;
    private readonly EventTypeRegistry _eventTypeRegistry;
    private readonly IntegreationEventMessageHandler _messageHandler;
    private readonly Dictionary<string, List<IIntegrationEventHandler>> _resolvedMockHandlers;

    public IntegrationEventMessageHandlerTests()
    {
        _resolvedMockHandlers = [];
        _fakeLogger = new FakeLogger<IntegreationEventMessageHandler>();

        _eventTypeRegistry = new EventTypeRegistry();

        _eventTypeRegistry.RegisterIntegrationEventsFromAssemblyTypes(
            [
                typeof(TestIntegrationEvent), 
                typeof(RegisteredTestIntegrationEventWithNoHandlers)
            ]);
    
        var services = CreateServiceCollectionWithDefaultServices();
        var serviceProvider = services.BuildServiceProvider();
        _messageHandler = serviceProvider.GetRequiredService<IntegreationEventMessageHandler>();
    }

    private ServiceCollection CreateServiceCollectionWithDefaultServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<IntegreationEventMessageHandler>>(_fakeLogger); 
        services.AddSingleton<IEventTypeRegistry>(_eventTypeRegistry); 
        RegisterMockHandlerForEventType<TestIntegrationEvent>(services, "DefaultHandler");
        services.AddScoped<IntegreationEventMessageHandler>();
        return services;
    }
    
    [Fact]
    public async Task HandleMessageAsync_ShouldReturnSuccess_WhenValidEventIsProcessed()
    {
        // Arrange
        var messageId = "test-message-123";
        var testEvent = new TestIntegrationEvent 
        { 
            EventId = Guid.NewGuid(),
            Data = "test-data"
        };
        
        var cancellationToken = TestContext.Current.CancellationToken;
        
        // Act
        var result = await _messageHandler.HandleMessageAsync(messageId, testEvent.ToBinaryData(), cancellationToken);
        
        // Assert
        result.Status.ShouldBe(MessageResultStatus.Success);
        result.ReasonCode.ShouldBeEmpty();
        _resolvedMockHandlers["DefaultHandler"].Count.ShouldBe(1);
        var mockHandler = _resolvedMockHandlers["DefaultHandler"].First();

        await mockHandler.Received(1).HandleAsync(
            Arg.Is<IIntegrationEvent>(e => ((TestIntegrationEvent)e).EventId == testEvent.EventId),
            cancellationToken);
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Level == LogLevel.Information && log.Message.Contains("Handling event"));
        logs.ShouldContain(log => log.Level == LogLevel.Information && log.Message.Contains("Successfully processed"));
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldReturnSkip_WhenEventTypeNotRegistered()
    {
        // Arrange
        var messageId = "unregistered-message-123";
        var testEvent = new UnregisteredTestIntegrationEvent { EventId = Guid.NewGuid() };
        
        var cancellationToken = TestContext.Current.CancellationToken;
        
        // Act
        var result = await _messageHandler.HandleMessageAsync(messageId, testEvent.ToBinaryData(), cancellationToken);
        
        // Assert
        result.Status.ShouldBe(MessageResultStatus.Skip);
        result.ReasonCode.ShouldBe("UnregisteredIntegrationEventType");
        result.Description.ShouldContain("not a registered integration event");
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Level == LogLevel.Warning && log.Message.Contains("UnregisteredIntegrationEventType"));
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldReturnDeadLetter_WhenJsonDeserializationFails()
    {
        // Arrange
        var messageId = "invalid-json-message-123";
        var cloudEvent = new CloudEvent(
            source: "test-source",
            type: nameof(TestIntegrationEvent),
            data: BinaryData.FromString("{ invalid json }"),
            dataContentType: "application/json");
        
        var cloudEventJson = JsonSerializer.Serialize(cloudEvent);
        var messageBody = BinaryData.FromString(cloudEventJson);
        var cancellationToken = TestContext.Current.CancellationToken;
        
        // Act
        var result = await _messageHandler.HandleMessageAsync(messageId, messageBody, cancellationToken);
        
        // Assert
        result.Status.ShouldBe(MessageResultStatus.DeadLetter);
        result.ReasonCode.ShouldBe("InvalidJsonFormat");
        result.Description.ShouldContain("JSON Deserialization failed");
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Level == LogLevel.Error && log.Message.Contains("InvalidJsonFormat"));
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldReturnDeadLetter_WhenDeserializedEventIsNull()
    {
        // Arrange
        var messageId = "null-event-message-123";
        var cloudEvent = new CloudEvent(
            source: "test-source",
            type: nameof(TestIntegrationEvent),
            data: BinaryData.FromString("null"),
            dataContentType: "application/json");
        
        var cloudEventJson = JsonSerializer.Serialize(cloudEvent);
        var messageBody = BinaryData.FromString(cloudEventJson);
        var cancellationToken = TestContext.Current.CancellationToken;
        
        // Act
        var result = await _messageHandler.HandleMessageAsync(messageId, messageBody, cancellationToken);
        
        // Assert
        result.Status.ShouldBe(MessageResultStatus.DeadLetter);
        result.ReasonCode.ShouldBe("DeserializationResultedInNull");
        result.Description.ShouldContain("Failed to deserialize CloudEvent data");
        
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Level == LogLevel.Error && log.Message.Contains("DeserializationResultedInNull"));
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldReturnSkip_WhenNoHandlersRegistered()
    {
        // Arrange     
        var messageId = "no-handlers-message-123";
        var testEvent = new RegisteredTestIntegrationEventWithNoHandlers { EventId = Guid.NewGuid() };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        // Act
        var result = await _messageHandler.HandleMessageAsync(messageId, testEvent.ToBinaryData(), cancellationToken);
        
        // Assert
        result.Status.ShouldBe(MessageResultStatus.Skip);
        result.ReasonCode.ShouldBe("NoRegisteredHandlers");
        result.Description.ShouldContain("No handlers registered");

        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldContain(log => log.Level == LogLevel.Warning && log.Message.Contains("NoRegisteredHandlers"));
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldCallMultipleHandlers_WhenMultipleHandlersRegistered()
    {
        // Arrange
        var services = CreateServiceCollectionWithDefaultServices();

        RegisterMockHandlerForEventType<TestIntegrationEvent>(services, "SecondHandler");
        RegisterMockHandlerForEventType<TestIntegrationEvent>(services, "ThirdHandler");

        var serviceProvider = services.BuildServiceProvider();
        var messageHandler = serviceProvider.GetRequiredService<IntegreationEventMessageHandler>();
        
        var messageId = "multiple-handlers-message-123";
        var testEvent = new TestIntegrationEvent 
        { 
            EventId = Guid.NewGuid(),
            Data = "test-data"
        };
        
        var cancellationToken = TestContext.Current.CancellationToken;
 
        // Act
        var result = await messageHandler.HandleMessageAsync(messageId, testEvent.ToBinaryData(), cancellationToken);
        
        // Assert
        result.Status.ShouldBe(MessageResultStatus.Success);
        _resolvedMockHandlers["DefaultHandler"].Count.ShouldBe(1);
        _resolvedMockHandlers["SecondHandler"].Count.ShouldBe(1);
        _resolvedMockHandlers["ThirdHandler"].Count.ShouldBe(1);

        foreach (var handler in _resolvedMockHandlers.Values.SelectMany(h => h))
        {
            await handler.Received(1).HandleAsync(
                Arg.Is<IIntegrationEvent>(e => ((TestIntegrationEvent)e).EventId == testEvent.EventId),
                cancellationToken);
        }
    }

    [Fact]
    public async Task HandleMessageAsync_ShouldCreateAndDisposeScope_ForEachMessage()
    {
        // Arrange
        var resolvedRealHandlers = new List<TestHandlerWithScopedContext>();
        var services = CreateServiceCollectionWithDefaultServices();
        services.AddScoped<ScopedContextObject>();
        services.AddScoped<IIntegrationEventHandler<TestIntegrationEvent>>(sp =>
        {
            var handler = new TestHandlerWithScopedContext(sp.GetRequiredService<ScopedContextObject>());
            resolvedRealHandlers.Add(handler);
            return handler;
        });

        var serviceProvider = services.BuildServiceProvider();
        var messageHandler = serviceProvider.GetRequiredService<IntegreationEventMessageHandler>();

        var testEvent1 = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "Event1" };
        var testEvent2 = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "Event2" };
        var cancellationToken = TestContext.Current.CancellationToken;
        
        // Act
        await messageHandler.HandleMessageAsync("Id1", testEvent1.ToBinaryData(), cancellationToken);
        await messageHandler.HandleMessageAsync("Id2", testEvent2.ToBinaryData(), cancellationToken);

        // Assert
        resolvedRealHandlers.Count().ShouldBe(2);
        resolvedRealHandlers[0].ScopedContextObject.Id.ShouldNotBe(resolvedRealHandlers[1].ScopedContextObject.Id); 
    }

    public record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }

    public record UnregisteredTestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }

    public record RegisteredTestIntegrationEventWithNoHandlers : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }
    public class TestHandlerWithScopedContext(ScopedContextObject scopedContextObject) : IntegrationEventHandler<TestIntegrationEvent>
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
        public ScopedContextObject ScopedContextObject { get; init; } = scopedContextObject;

        public override Task<Result> HandleAsync(TestIntegrationEvent input, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result.Success());
        }
    }
    public class ScopedContextObject{ public Guid Id { get; init; } = Guid.NewGuid(); }
    private void RegisterMockHandlerForEventType<TEvent>(IServiceCollection services, string handlerKey) where TEvent : IIntegrationEvent
    {
        services.AddScoped<IIntegrationEventHandler<TEvent>>(sp =>
        {
            var mockHandler = Substitute.For<IIntegrationEventHandler<TEvent>>();
            mockHandler.EventType.Returns(typeof(TEvent));
            mockHandler.HandleAsync(Arg.Any<IIntegrationEvent>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());
            if (!_resolvedMockHandlers.ContainsKey(handlerKey))
                _resolvedMockHandlers[handlerKey] = [];
            _resolvedMockHandlers[handlerKey].Add(mockHandler);
            return mockHandler;
        });
    }
}

internal static class TestExtensions
{

    public static BinaryData ToBinaryData(this IntegrationEventBase integrationEvent)
    {
        var cloudEvent = integrationEvent.ToCloudEvent();
        var cloudEventJson = JsonSerializer.Serialize(cloudEvent);
        return BinaryData.FromString(cloudEventJson);
    } 

}