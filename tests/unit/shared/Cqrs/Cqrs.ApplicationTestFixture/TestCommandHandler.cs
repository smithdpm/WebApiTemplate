using Ardalis.Result;
using Cqrs.Operations.Commands;

namespace Cqrs.ApplicationTestFixture;

public class TestCommandHandler : CommandHandler<TestCommand, string>
{
    public override Task<Result<string>> HandleAsync(TestCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Success("Success"));
    }
}