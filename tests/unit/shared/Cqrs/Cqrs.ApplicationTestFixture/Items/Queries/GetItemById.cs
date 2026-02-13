using Ardalis.Result;
using Cqrs.ApplicationTestFixture.Database;
using Cqrs.DomainTestFixture;
using Cqrs.Operations.Queries;

namespace Cqrs.ApplicationTestFixture.Items.Queries;

internal class GetItemById
{
}
public record GetItemByIdQuery(Guid Id) : IQuery<Item>;

public class GetItemByIdQueryHandler(ApplicationDbContext dbContext) : QueryHandler<GetItemByIdQuery, Item>
{
    public override async Task<Result<Item>> HandleAsync(GetItemByIdQuery query, CancellationToken cancellationToken)
    {
        var item = await dbContext.FindAsync<Item>(new object?[] { query.Id }, cancellationToken);

        if (item == null)
        {
            return Result<Item>.NotFound();
        }   
        return Result<Item>.Success(item);
    }
}