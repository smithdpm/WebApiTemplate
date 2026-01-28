using Cqrs.Abstractions.Events;
using Cqrs.Events;
using Cqrs.Events.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using SharedKernel.Events;
using Shouldly;

namespace Cqrs.UnitTests.Events;

public class DomainEventDispatcherTests
{
    private readonly IServiceCollection _services;
    private readonly Dictionary<string, List<IDomainEventHandler>> _resolvedMockHandlers;
    public DomainEventDispatcherTests()
    {
        _services = new ServiceCollection();
        _resolvedMockHandlers = new Dictionary<string, List<IDomainEventHandler>>();
    }

    [Fact]
    public async Task DispatchEventsAsync_ShouldInvokeHandlers_WhenHandlersRegistered()
    {
        // Arrange
        _services.AddFakeLogging();

        var domainEvent = new TestDomainEvent { EntityId = Guid.NewGuid(), Name = "Test" };

        RegisterMockHandlerForEventType<TestDomainEvent>("handler1");
        
        _services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        
        var serviceProvider = _services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDomainEventDispatcher>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await dispatcher.DispatchEventsAsync(new[] { domainEvent }, cancellationToken);

        // Assert
        await _resolvedMockHandlers["handler1"][0].Received(1).HandleAsync(domainEvent, cancellationToken);
    }

    [Fact]
    public async Task DispatchEventsAsync_ShouldLogWarning_WhenNoHandlersRegistered()
    {
        // Arrange
        var fakeLogger = new FakeLogger<DomainEventDispatcher>();
        _services.AddSingleton<ILogger<DomainEventDispatcher>>(_ => fakeLogger);
        var domainEvent = new TestDomainEvent { EntityId = Guid.NewGuid(), Name = "Test" };

        _services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        
        var serviceProvider = _services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDomainEventDispatcher>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await dispatcher.DispatchEventsAsync(new[] { domainEvent }, cancellationToken);

        // Assert
        var log = fakeLogger.Collector.LatestRecord;
        log.Level.ShouldBe(LogLevel.Warning);
        log.Message.ShouldContain("No handlers registered");
        log.Message.ShouldContain(nameof(TestDomainEvent));
    }

    [Fact]
    public async Task DispatchEventsAsync_ShouldCreateNewScope_ForEachEvent()
    {
        // Arrange
        var event1 = new TestDomainEvent { EntityId = Guid.NewGuid(), Name = "Event1" };
        var event2 = new TestDomainEvent { EntityId = Guid.NewGuid(), Name = "Event2" };
        var events = new[] { event1, event2 };

        RegisterMockHandlerForEventType<TestDomainEvent>("handler1");
        
        _services.AddFakeLogging();
        
        _services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        var serviceProvider = _services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDomainEventDispatcher>();

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await dispatcher.DispatchEventsAsync(events, cancellationToken);

        // Assert
        _resolvedMockHandlers["handler1"].Count.ShouldBe(2);
        await _resolvedMockHandlers["handler1"][0].Received(1).HandleAsync(event1, cancellationToken);
        await _resolvedMockHandlers["handler1"][1].Received(1).HandleAsync(event2, cancellationToken);
    }

    [Fact]
    public async Task DispatchEventsAsync_ShouldInvokeMultipleHandlers_WhenMultipleHandlersForSameEventType()
    {
        // Arrange
        var domainEvent = new TestDomainEvent { EntityId = Guid.NewGuid(), Name = "Test" };
        
        RegisterMockHandlerForEventType<TestDomainEvent>("handler1");
        RegisterMockHandlerForEventType<TestDomainEvent>("handler2");
        RegisterMockHandlerForEventType<AnotherTestDomainEvent>("handler3");

        _services.AddFakeLogging();
        _services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

        var serviceProvider = _services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDomainEventDispatcher>();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        await dispatcher.DispatchEventsAsync(new[] { domainEvent }, cancellationToken);

        // Assert
        await _resolvedMockHandlers["handler1"][0].Received(1).HandleAsync(domainEvent, cancellationToken);
        await _resolvedMockHandlers["handler2"][0].Received(1).HandleAsync(domainEvent, cancellationToken);
        await _resolvedMockHandlers["handler3"][0].DidNotReceive().HandleAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
    }

    private void RegisterMockHandlerForEventType<TEvent>(string handlerKey) where TEvent : IDomainEvent
    {
        _services.AddScoped<IDomainEventHandler>(sp => {
            var mockHandler = Substitute.For<IDomainEventHandler>();
            mockHandler.EventType.Returns(typeof(TEvent));
            if (!_resolvedMockHandlers.ContainsKey(handlerKey))
                _resolvedMockHandlers[handlerKey] = new List<IDomainEventHandler>();
            _resolvedMockHandlers[handlerKey].Add(mockHandler);
            return mockHandler;
        });
    }
    public record TestDomainEvent : IDomainEvent
    {
        public Guid EntityId { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public record AnotherTestDomainEvent : IDomainEvent
    {
        public Guid EntityId { get; init; }
    }
}