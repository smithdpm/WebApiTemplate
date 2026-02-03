using Ardalis.Result;
using Cqrs.ApplicationTestFixture.Database;
using Cqrs.DomainTestFixture;
using Cqrs.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.ApplicationTestFixture.Items.Commands;

public record AddItemCommand(string Name): ICommand<Guid>;

public class AddItemCommandHandler(ApplicationDbContext dbContext) : ICommandHandler<AddItemCommand, Guid>
{
    public Task<Result<Guid>> HandleAsync(AddItemCommand command, CancellationToken cancellationToken)
    {
        var newItem = new Item(command.Name);
        dbContext.Add(newItem);
        return Task.FromResult(Result<Guid>.Success(newItem.Id));
    }
}

