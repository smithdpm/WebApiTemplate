using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.ApplicationTestFixture;

public class TestQueryHandler : QueryHandler<TestQuery, string>
{
    public override Task<Result<string>> HandleAsync(TestQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Success("Success"));
    }
}