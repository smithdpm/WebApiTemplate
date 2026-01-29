using Ardalis.Result;
using Cqrs.ApplicationTestFixture.Database;
using Cqrs.DomainTestFixture;
using Cqrs.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cqrs.ApplicationTestFixture.Items.Queries;

internal class GetItemById
{
}
public record GetItemByIdQuery(Guid Id) : IQuery<Item>;

public class GetItemByIdQueryHandler(ApplicationDbContext dbContext) : IQueryHandler<GetItemByIdQuery, Item>
{
    async Task<Result<Item>> IQueryHandler<GetItemByIdQuery, Item>.Handle(GetItemByIdQuery query, CancellationToken cancellationToken)
    {
        var item = await dbContext.FindAsync<Item>(new object?[] { query.Id }, cancellationToken);

        if (item == null)
        {
            return Result<Item>.NotFound();
        }   
        return Result<Item>.Success(item);
    }
}