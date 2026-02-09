using Ardalis.Result;
using Cqrs.Events.DomainEvents;
using Cqrs.Messaging;
using Shop.Application.IntegrationEvents.Events;
using Shop.Application.Stock;
using Shop.Domain.DomainEvents;

namespace Shop.Application.DomainEventHandlers;

public class LowStockDomainEventHandler(ICommandHandler<UpdateStockCommand> updateStockHandler) 
    : DomainEventHandler<LowStockDomainEvent>
{
    public override async Task<Result> HandleAsync(LowStockDomainEvent input, CancellationToken cancellationToken)
    {
        int stockAmountToAdd = 50;
        var updateStockCommand = new UpdateStockCommand(input.ProductName, stockAmountToAdd);
        await updateStockHandler.HandleAsync(updateStockCommand, cancellationToken);
        AddIntegrationEvent(new StockAddedIntegrationEvent(input.ProductName, stockAmountToAdd));
        return Result.Success();
    }
}
