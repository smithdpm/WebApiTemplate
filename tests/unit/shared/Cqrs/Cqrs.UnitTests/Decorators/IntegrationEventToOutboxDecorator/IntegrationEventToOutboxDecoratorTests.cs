using Ardalis.Result;
using Cqrs.Decorators;
using Cqrs.Decorators.IntegrationEventToOutboxDecorator;
using Cqrs.Events.IntegrationEvents;
using Cqrs.Messaging;
using Cqrs.Outbox;
using NSubstitute;
using SharedKernel.Database;
using Shouldly;
using System.Text.Json;

namespace Cqrs.UnitTests.Decorators.IntegrationEventToOutboxDecorator;

public class IntegrationEventToOutboxDecoratorTests
{
    private readonly IRepository<OutboxMessage> _repository;
    private readonly IIntegrationEventToOutboxBehaviour _integrationEventBehaviour;

    public IntegrationEventToOutboxDecoratorTests()
    {
        _repository = Substitute.For<IRepository<OutboxMessage>>();
        _integrationEventBehaviour = new IntegrationEventToOutboxBehaviour(_repository);
    }

    public class CommandHandlerWithResponseTests : IntegrationEventToOutboxDecoratorTests
    {
        [Fact]
        public async Task Handle_ShouldCaptureIntegrationEvents_WhenCommandSucceeds()
        {
            // Arrange
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventToOutboxCommandDecorator<TestCommand, string>(innerHandler, _integrationEventBehaviour);
            var command = new TestCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            innerHandler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" }, "topic1");
            innerHandler.SetResult(Result<string>.Success("Success"));

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _repository.Received(1).AddRangeAsync(
                Arg.Is<List<OutboxMessage>>(messages => 
                    messages.Count == 1 && 
                    messages[0].Destination == "topic1" &&
                    messages[0].EventType == nameof(TestIntegrationEvent)),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldNotCaptureEvents_WhenCommandFails()
        {
            // Arrange
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventToOutboxCommandDecorator<TestCommand, string>(innerHandler, _integrationEventBehaviour);
            var command = new TestCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            innerHandler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" }, "topic1");
            innerHandler.SetResult(Result<string>.Error("Command failed"));

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldPassThroughResult_WhenDecoratingHandler()
        {
            // Arrange
            var expectedResult = "Expected Result";
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventToOutboxCommandDecorator<TestCommand, string>(innerHandler, _integrationEventBehaviour);
            var command = new TestCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            innerHandler.SetResult(Result<string>.Success(expectedResult));

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
        }

        [Fact]
        public async Task Handle_ShouldHandleNoEvents_WhenHandlerHasEventsToSend()
        {
            // Arrange
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventToOutboxCommandDecorator<TestCommand, string>(innerHandler, _integrationEventBehaviour);
            var command = new TestCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            innerHandler.SetResult(Result<string>.Success("Success"));

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldSetCorrectDestination_WhenEventsHaveDestination()
        {
            // Arrange
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventToOutboxCommandDecorator<TestCommand, string>(innerHandler, _integrationEventBehaviour);
            var command = new TestCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            var destination = "order-events-topic";
            var integrationEvent = new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "order-created" };
            innerHandler.AddIntegrationEvent(integrationEvent, destination);
            innerHandler.SetResult(Result<string>.Success("Success"));

            List<OutboxMessage> capturedMessages = null;
            await _repository.AddRangeAsync(Arg.Do<List<OutboxMessage>>(messages => capturedMessages = messages), Arg.Any<CancellationToken>());

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            capturedMessages.ShouldNotBeNull();
            capturedMessages.Count.ShouldBe(1);
            capturedMessages[0].Destination.ShouldBe(destination);
            
            var payload = JsonSerializer.Deserialize<TestIntegrationEvent>(capturedMessages[0].Payload)!;
            payload.EventId.ShouldBe(integrationEvent.EventId);
            payload.Data.ShouldBe(integrationEvent.Data);
        }

        [Fact]
        public async Task Handle_ShouldHandleMultipleDestinations_WhenEventsForDifferentTopics()
        {
            // Arrange
            var innerHandler = new TestCommandHandlerWithEvents<TestCommand, string>();
            var decorator = new IntegrationEventToOutboxCommandDecorator<TestCommand, string>(innerHandler, _integrationEventBehaviour);
            var command = new TestCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            innerHandler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event1" }, "topic1");
            innerHandler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event2" }, "topic2");
            innerHandler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "event3" }, "topic1");
            innerHandler.SetResult(Result<string>.Success("Success"));

            List<OutboxMessage> capturedMessages = null;
            await _repository.AddRangeAsync(Arg.Do<List<OutboxMessage>>(messages => capturedMessages = messages), Arg.Any<CancellationToken>());

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

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

    public class CommandHandlerWithoutResponseTests : IntegrationEventToOutboxDecoratorTests
    {
        [Fact]
        public async Task Handle_ShouldCaptureIntegrationEvents_WhenCommandSucceeds()
        {
            // Arrange
            var innerHandler = new TestVoidCommandHandlerWithEvents<TestVoidCommand>();
            var decorator = new IntegrationEventToOutboxCommandDecorator<TestVoidCommand>(innerHandler, _integrationEventBehaviour);
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            innerHandler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" }, "topic1");
            innerHandler.SetResult(Result.Success());

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            await _repository.Received(1).AddRangeAsync(
                Arg.Is<List<OutboxMessage>>(messages => 
                    messages.Count == 1 && 
                    messages[0].Destination == "topic1" &&
                    messages[0].EventType == nameof(TestIntegrationEvent)),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ShouldNotCaptureEvents_WhenCommandFails()
        {
            // Arrange
            var innerHandler = new TestVoidCommandHandlerWithEvents<TestVoidCommand>();
            var decorator = new IntegrationEventToOutboxCommandDecorator<TestVoidCommand>(innerHandler, _integrationEventBehaviour);
            var command = new TestVoidCommand { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            innerHandler.AddIntegrationEvent(new TestIntegrationEvent { EventId = Guid.NewGuid(), Data = "test" }, "topic1");
            innerHandler.SetResult(Result.Error("Command failed"));

            // Act
            var result = await decorator.HandleAsync(command, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            await _repository.DidNotReceive().AddRangeAsync(Arg.Any<List<OutboxMessage>>(), Arg.Any<CancellationToken>());
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

    private class TestCommandHandlerWithEvents<TCommand, TResponse> : CommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        private Result<TResponse> _result = null!;

        public void SetResult(Result<TResponse> result)
        {
            _result = result;
        }

        public override Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }

    private class TestVoidCommandHandlerWithEvents<TCommand> : CommandHandler<TCommand>
        where TCommand : ICommand
    {
        private Result _result = null!;

        public void SetResult(Result result)
        {
            _result = result;
        }

        public override Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
        {
            return Task.FromResult(_result);
        }
    }
}