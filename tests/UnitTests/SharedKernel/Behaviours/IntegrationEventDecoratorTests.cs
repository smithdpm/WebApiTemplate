using Ardalis.Result;
using Cqrs.Decorators;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Messaging;
using Cqrs.Outbox;
using NSubstitute;
using SharedKernel.Database;
using Shouldly;
using System.Text.Json;

namespace UnitTests.SharedKernel.Behaviours;

public class IntegrationEventDecoratorTests
{
    private readonly IRepository<OutboxMessage> _repository;

    public IntegrationEventDecoratorTests()
    {
        _repository = Substitute.For<IRepository<OutboxMessage>>();
    }

    public class CommandHandlerWithResponseTests : IntegrationEventDecoratorTests
    {
        [Fact]
        public async Task Handle_ShouldCaptureIntegrationEvents_WhenCommandSucceeds()
        {
            // Arrange
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventDecorator<TestCommand, string>(innerHandler, _repository);
            var command = new TestCommand { Id = Guid.NewGuid() };
            
            innerHandler.AddIntegrationEvent("topic1", new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" });
            innerHandler.SetResult(Result<string>.Success("Success"));

            // Act
            var result = await decorator.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _repository.Received(1).AddRangeAsync(
                Arg.Is<List<OutboxMessage>>(messages => 
                    messages.Count == 1 && 
                    messages[0].Destination == "topic1" &&
                    messages[0].EventType == nameof(TestIntegrationEvent)));
        }

        [Fact]
        public async Task Handle_ShouldNotCaptureEvents_WhenCommandFails()
        {
            // Arrange
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventDecorator<TestCommand, string>(innerHandler, _repository);
            var command = new TestCommand { Id = Guid.NewGuid() };
            
            innerHandler.AddIntegrationEvent("topic1", new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" });
            innerHandler.SetResult(Result<string>.Error("Command failed"));

            // Act
            var result = await decorator.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>());
        }

        [Fact]
        public async Task Handle_ShouldPassThroughResult_WhenDecoratingHandler()
        {
            // Arrange
            var expectedResult = "Expected Result";
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventDecorator<TestCommand, string>(innerHandler, _repository);
            var command = new TestCommand { Id = Guid.NewGuid() };
            
            innerHandler.SetResult(Result<string>.Success(expectedResult));

            // Act
            var result = await decorator.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
        }

        [Fact]
        public async Task Handle_ShouldHandleNoEvents_WhenHandlerNotImplementIHasIntegrationEvents()
        {
            // Arrange
            var innerHandler = new TestCommandHandlerWithoutEvents<TestCommand, string>();
            var decorator = new IntegrationEventDecorator<TestCommand, string>(innerHandler, _repository);
            var command = new TestCommand { Id = Guid.NewGuid() };
            
            innerHandler.SetResult(Result<string>.Success("Success"));

            // Act
            var result = await decorator.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>());
        }

        [Fact]
        public async Task Handle_ShouldSetCorrectDestination_WhenEventsHaveDestination()
        {
            // Arrange
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventDecorator<TestCommand, string>(innerHandler, _repository);
            var command = new TestCommand { Id = Guid.NewGuid() };
            
            var destination = "order-events-topic";
            var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "order-created" };
            innerHandler.AddIntegrationEvent(destination, integrationEvent);
            innerHandler.SetResult(Result<string>.Success("Success"));

            List<OutboxMessage> capturedMessages = null;
            await _repository.AddRangeAsync(Arg.Do<List<OutboxMessage>>(messages => capturedMessages = messages));

            // Act
            var result = await decorator.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            capturedMessages.ShouldNotBeNull();
            capturedMessages.Count.ShouldBe(1);
            capturedMessages[0].Destination.ShouldBe(destination);
            
            var payload = JsonSerializer.Deserialize<TestIntegrationEvent>(capturedMessages[0].Payload);
            payload.EventId.ShouldBe(integrationEvent.EventId);
            payload.Data.ShouldBe(integrationEvent.Data);
        }

        [Fact]
        public async Task Handle_ShouldHandleMultipleDestinations_WhenEventsForDifferentTopics()
        {
            // Arrange
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventDecorator<TestCommand, string>(innerHandler, _repository);
            var command = new TestCommand { Id = Guid.NewGuid() };
            
            innerHandler.AddIntegrationEvent("topic1", new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event1" });
            innerHandler.AddIntegrationEvent("topic2", new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event2" });
            innerHandler.AddIntegrationEvent("topic1", new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event3" });
            innerHandler.SetResult(Result<string>.Success("Success"));

            List<OutboxMessage> capturedMessages = null;
            await _repository.AddRangeAsync(Arg.Do<List<OutboxMessage>>(messages => capturedMessages = messages));

            // Act
            var result = await decorator.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            capturedMessages.ShouldNotBeNull();
            capturedMessages.Count.ShouldBe(3);
            
            var topic1Messages = capturedMessages.Where(m => m.Destination == "topic1").ToList();
            var topic2Messages = capturedMessages.Where(m => m.Destination == "topic2").ToList();
            
            topic1Messages.Count.ShouldBe(2);
            topic2Messages.Count.ShouldBe(1);
        }
    }

    public class CommandHandlerWithoutResponseTests : IntegrationEventDecoratorTests
    {
        [Fact]
        public async Task Handle_ShouldCaptureIntegrationEvents_WhenCommandSucceeds()
        {
            // Arrange
            var innerHandler = new TestVoidCommandHandlerWithEvents<TestVoidCommand>();
            var decorator = new IntegrationEventDecorator<TestVoidCommand>(innerHandler, _repository);
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            
            innerHandler.AddIntegrationEvent("topic1", new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" });
            innerHandler.SetResult(Result.Success());

            // Act
            var result = await decorator.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _repository.Received(1).AddRangeAsync(
                Arg.Is<List<OutboxMessage>>(messages => 
                    messages.Count == 1 && 
                    messages[0].Destination == "topic1" &&
                    messages[0].EventType == nameof(TestIntegrationEvent)));
        }

        [Fact]
        public async Task Handle_ShouldNotCaptureEvents_WhenCommandFails()
        {
            // Arrange
            var innerHandler = new TestVoidCommandHandlerWithEvents<TestVoidCommand>();
            var decorator = new IntegrationEventDecorator<TestVoidCommand>(innerHandler, _repository);
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            
            innerHandler.AddIntegrationEvent("topic1", new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" });
            innerHandler.SetResult(Result.Error("Command failed"));

            // Act
            var result = await decorator.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>());
        }
    }

    private class TestCommand : ICommand<string>
    {
        public Guid Id { get; set; }
    }

    private class TestVoidCommand : ICommand
    {
        public Guid Id { get; set; }
    }

    private record TestIntegrationEvent : IntegrationEventBase
    {
        public Guid EventId { get; init; }
        public string Data { get; init; } = string.Empty;
    }

    private class TestCommandHandlerWithEvents<TCommand, TResponse> : HasIntegrationEvents, ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        private Result<TResponse> _result = null!;

        public void SetResult(Result<TResponse> result)
        {
            _result = result;
        }

        public Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }

    private class TestVoidCommandHandlerWithEvents<TCommand> : HasIntegrationEvents, ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private Result _result = null!;

        public void SetResult(Result result)
        {
            _result = result;
        }

        public Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }

    private class TestCommandHandlerWithoutEvents<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        private Result<TResponse> _result = null!;

        public void SetResult(Result<TResponse> result)
        {
            _result = result;
        }

        public Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }
}