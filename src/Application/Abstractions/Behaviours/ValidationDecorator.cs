using Application.Abstractions.Messaging;
using FluentValidation;
using FluentValidation.Results;
using SharedKernel;
using System.Threading;

namespace Application.Abstractions.Behaviours;

internal static class ValidationDecorator
{
    internal sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await ValidateAsync(command, validators, cancellationToken);
            if (validationResult.Any())
            {
                return Result.Failure<TResponse>(CreateValidationError(validationResult));
            }
            return await innerHandler.Handle(command, cancellationToken);
        }
    }
    internal sealed class CommandHandler<TCommand>(ICommandHandler<TCommand> innerHandler,
            IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand>
            where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            var validationResult = await ValidateAsync(command, validators, cancellationToken);
            if (validationResult.Any())
            {
                return Result.Failure(CreateValidationError(validationResult));
            }
            return await innerHandler.Handle(command, cancellationToken);
        }
    }

    private static async Task<ValidationFailure[]> ValidateAsync<TCommand>(TCommand command, IEnumerable<IValidator<TCommand>> validators, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return [];
        }

        var validationContext = new ValidationContext<TCommand>(command);

        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(validationContext, cancellationToken))
        );

        var validationFailures = results.Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .ToArray();

        return validationFailures;
    }

    private static ValidationError CreateValidationError(ValidationFailure[] validationFailures) =>
        new(validationFailures.Select(f => Error.Problem(f.ErrorCode, f.ErrorMessage)).ToArray());
}
