using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.ApplicationTestFixture;

public class TestCommandHandlerNoResponse : ICommandHandler<TestCommandNoResponse>
{
    public Task<Result> Handle(TestCommandNoResponse command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }
}