using Ardalis.Result;
using Cqrs.Extensions;
using Cqrs.Operations.Commands;
using FluentValidation;

namespace Cqrs.Decorators.ValidationDecorator;

public class ValidationCommandDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    IEnumerable<IValidator<TCommand>> validators
    ) : CommandHandlerDecorator<TCommand, TResponse>(innerHandler)
    where TCommand : ICommand<TResponse>
{
    public async override Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await ValidationExtensions.ValidateAsync(command, validators, cancellationToken);
        if (validationResult.Any())
        {
            return Result<TResponse>.Invalid(validationResult);
        }

        return await HandleInner(command, cancellationToken);
    }
}

public class ValidationCommandDecorator<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    IEnumerable<IValidator<TCommand>> validators
    ) : CommandHandlerDecorator<TCommand>(innerHandler)
    where TCommand : ICommand
{
    public async override Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await ValidationExtensions.ValidateAsync(command, validators, cancellationToken);
        if (validationResult.Any())
        {
            return Result.Invalid(validationResult);
        }

        return await HandleInner(command, cancellationToken);
    }
}
