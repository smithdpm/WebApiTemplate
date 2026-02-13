using Ardalis.Result;
using Cqrs.ApplicationTestFixture.Database;
using Cqrs.DomainTestFixture;
using Cqrs.Operations.Commands;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.ApplicationTestFixture.Items.Commands;

public record UpdateItemNameCommand(Guid Id, string NewName) : ICommand;

public class UpdateItemNameCommandHandler(ApplicationDbContext dbContext) : ICommandHandler<UpdateItemNameCommand>
{
    public async Task<Result> HandleAsync(UpdateItemNameCommand command, CancellationToken cancellationToken)
    {

        var item = await dbContext.FindAsync<Item>(command.Id, cancellationToken);

        if (item == null)
        {
            return Result.NotFound($"Item with Id {command.Id} was not found.");
        }

        item.UpdateName(command.NewName);
        return Result.Success();
    }
}