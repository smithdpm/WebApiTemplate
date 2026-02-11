using Ardalis.Result;
using Cqrs.Decorators.IntegrationEventToOutboxDecorator;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Messaging;
using Cqrs.Outbox;
using NSubstitute;
using SharedKernel.Database;
using Shouldly;
using System.Text.Json;

namespace Cqrs.UnitTests.Decorators.IntegrationEventToOutboxDecorator;

public class IntegrationEventToOutboxBehaviourTests
{
    private readonly IRepository<OutboxMessage> _repository;
    private readonly IntegrationEventToOutboxBehaviour _behaviour;

    public IntegrationEventToOutboxBehaviourTests()
    {
        _repository = Substitute.For<IRepository<OutboxMessage>>();
        _behaviour = new IntegrationEventToOutboxBehaviour(_repository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddEventsToOutbox_WhenResultIsSuccessAndHasEvents()
    {
        // Arrange
        var handler = new TestHandlerWithEvents();
        var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" };
        handler.AddIntegrationEvent(integrationEvent, "test-topic");
        
        var input = "test-input";
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _behaviour.ExecuteAsync(handler, input, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _repository.Received(1).AddRangeAsync(
            Arg.Is<List<OutboxMessage>>(messages => 
                messages.Count == 1 && 
                messages[0].Destination == "test-topic" &&
                messages[0].EventType == nameof(TestIntegrationEvent)),
            cancellationToken);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotAddEventsToOutbox_WhenResultIsError()
    {
        // Arrange
        var handler = new TestHandlerWithEvents();
        handler.SetResult(Result<string>.Error("Error"));
        handler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid() }, "test-topic");
        
        var input = "test-input";
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _behaviour.ExecuteAsync(handler, input, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotAddEventsToOutbox_WhenNoEventsToSend()
    {
        // Arrange
        var handler = new TestHandlerWithEvents();
        var input = "test-input";
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _behaviour.ExecuteAsync(handler, input, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldHandleMultipleEvents()
    {
        // Arrange
        var handler = new TestHandlerWithEvents();
        handler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event1" }, "topic1");
        handler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event2" }, "topic2");
        handler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event3" }, "topic1");
        
        var input = "test-input";
        var cancellationToken = TestContext.Current.CancellationToken;

        List<OutboxMessage>? capturedMessages = null;
        await _repository.AddRangeAsync(Arg.Do<List<OutboxMessage>>(messages => capturedMessages = messages), cancellationToken);

        // Act
        var result = await _behaviour.ExecuteAsync(handler, input, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        capturedMessages.ShouldNotBeNull();
        capturedMessages!.Count.ShouldBe(3);
        
        var topic1Messages = capturedMessages.Where(m => m.Destination == "topic1").ToList();
        var topic2Messages = capturedMessages.Where(m => m.Destination == "topic2").ToList();
        
        topic1Messages.Count.ShouldBe(2);
        topic2Messages.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOriginalResult()
    {
        // Arrange
        var handler = new TestHandlerWithEvents();
        var expectedResult = "Expected Result";
        handler.SetResult(Result<string>.Success(expectedResult));
        
        var input = "test-input";
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _behaviour.ExecuteAsync(handler, input, cancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedResult);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSerializeEventsCorrectly()
    {
        // Arrange
        var handler = new TestHandlerWithEvents();
        var integrationEvent = new TestIntegrationEvent 
        { 
            EventId = Guid.NewGuid(), 
            Data = "test-data-123" 
        };
        handler.AddIntegrationEvent(integrationEvent, "test-topic");
        
        var input = "test-input";
        var cancellationToken = TestContext.Current.CancellationToken;

        List<OutboxMessage>? capturedMessages = null;
        await _repository.AddRangeAsync(Arg.Do<List<OutboxMessage>>(messages => capturedMessages = messages), cancellationToken);

        // Act
        await _behaviour.ExecuteAsync(handler, input, cancellationToken);

        // Assert
        capturedMessages.ShouldNotBeNull();
        capturedMessages!.Count.ShouldBe(1);
        
        var payload = JsonSerializer.Deserialize<TestIntegrationEvent>(capturedMessages[0].Payload);
        payload.ShouldNotBeNull();
        payload!.EventId.ShouldBe(integrationEvent.EventId);
        payload.Data.ShouldBe(integrationEvent.Data);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPropagateException_WhenHandlerThrows()
    {
        // Arrange
        var handler = new TestHandlerWithException();
        var input = "test-input";
        var cancellationToken = TestContext.Current.CancellationToken;

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await _behaviour.ExecuteAsync(handler, input, cancellationToken));
        
        exception.Message.ShouldBe("Test exception");
        await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>(), Arg.Any<CancellationToken>());
    }

    private record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }

    private class TestHandlerWithEvents : HandlerBase<string, Result<string>>
    {
        private Result<string> _result = Result<string>.Success("Success");

        public void SetResult(Result<string> result)
        {
            _result = result;
        }

        public override Task<Result<string>> HandleAsync(string input, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }

    private class TestHandlerWithException : HandlerBase<string, Result<string>>
    {
        public override Task<Result<string>> HandleAsync(string input, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Test exception");
        }
    }
}