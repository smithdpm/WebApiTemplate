using Ardalis.Result;
using Cqrs.Operations.Commands;

namespace Cqrs.ApplicationTestFixture;

public class TestCommandHandlerNoResponse : ICommandHandler<TestCommandNoResponse>
{
    public Task<Result> HandleAsync(TestCommandNoResponse command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result.Success());
    }
}