using Ardalis.Result;
using Cqrs.Messaging;

namespace Cqrs.ApplicationTestFixture;

public class TestQueryHandler : IQueryHandler<TestQuery, string>
{
    public Task<Result<string>> HandleAsync(TestQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Success("Success"));
    }
}