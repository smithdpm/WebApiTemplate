using Ardalis.Result;
using Cqrs.Decorators;
using Cqrs.Messaging;
using Microsoft.AspNetCore.Mvc.Formatters;
using NSubstitute;
using Shouldly;

namespace Cqrs.UnitTests.Decorators;

public class HandlerDecoratorTests
{

        [Fact]
        public async Task HandleInner_ShouldCallInnerHandler()
        {
            // Arrange
            var innerHandler = Substitute.For<IHandler<TestInput, Result<string>>>();
            var expectedResult = "Expected Result";
            var input = new TestInput { Id = Guid.NewGuid() };
            var cancellationToken = TestContext.Current.CancellationToken;
            
            innerHandler.HandleAsync(input, cancellationToken)
                .Returns(Result<string>.Success(expectedResult));
            
            var decorator = new TestHandlerDecorator<TestInput, Result<string>>(innerHandler);

            // Act
            var result = await decorator.HandleAsync(input, cancellationToken);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Value.ShouldBe(expectedResult);
            await innerHandler.Received(1).HandleAsync(input, cancellationToken);
        }

    public class TestInput
    {
        public Guid Id { get; set; }
    }

    public class TestHandlerDecorator<TInput, TResult> : HandlerDecorator<TInput, TResult>
        where TResult : IResult
    {
        public TestHandlerDecorator(IHandler<TInput, TResult> innerHandler) 
            : base(innerHandler)
        {
        }

        public override Task<TResult> HandleAsync(TInput input, CancellationToken cancellationToken)
        {
            return HandleInner(input, cancellationToken);
        }
    }
}