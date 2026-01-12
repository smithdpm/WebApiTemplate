using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using FluentValidation;
using SharedKernel.Messaging;

namespace SharedKernel.Behaviours;

public class ValidationDecorator<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> innerHandler,
    IEnumerable<IValidator<TCommand>> validators
    ) : CommandHandlerDecorator<TCommand, TResponse>(innerHandler)
    where TCommand : ICommand<TResponse>
{
    public async override Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await ValidationLogic.ValidateAsync(command, validators, cancellationToken);
        if (validationResult.Any())
        {
            return Result<TResponse>.Invalid(validationResult);
        }

        return await HandleInner(command, cancellationToken);
    }
}

public class ValidationDecorator<TCommand>(
    ICommandHandler<TCommand> innerHandler,
    IEnumerable<IValidator<TCommand>> validators
    ) : CommandHandlerDecorator<TCommand>(innerHandler)
    where TCommand : ICommand
{
    public async override Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await ValidationLogic.ValidateAsync(command, validators, cancellationToken);
        if (validationResult.Any())
        {
            return Result.Invalid(validationResult);
        }

        return await HandleInner(command, cancellationToken);
    }
}

public static class ValidationLogic
{
    public static async Task<List<ValidationError>> ValidateAsync<TCommand>(TCommand command, IEnumerable<IValidator<TCommand>> validators, CancellationToken cancellationToken)
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
            .SelectMany(r => r.AsErrors())
            .ToList();

        return validationFailures;
    }
}
    

//public static class ValidationDecorator
//{
//    public sealed class CommandHandler<TCommand, TResponse>(
//        ICommandHandler<TCommand, TResponse> innerHandler,
//        IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand, TResponse>
//        where TCommand : ICommand<TResponse>
//    {
//        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
//        {
//            var validationResult = await ValidateAsync(command, validators, cancellationToken);
//            if (validationResult.Any())
//            {
//                return Result.Invalid(validationResult);
//            }
//            return await innerHandler.Handle(command, cancellationToken);
//        }
//    }
//    public sealed class CommandHandler<TCommand>(ICommandHandler<TCommand> innerHandler,
//            IEnumerable<IValidator<TCommand>> validators) : ICommandHandler<TCommand>
//            where TCommand : ICommand
//    {
//        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
//        {
//            var validationResult = await ValidateAsync(command, validators, cancellationToken);
//            if (validationResult.Any())
//            {
//                return Result.Invalid(validationResult);
//            }
//            return await innerHandler.Handle(command, cancellationToken);
//        }
//    }

//    private static async Task<List<ValidationError>> ValidateAsync<TCommand>(TCommand command, IEnumerable<IValidator<TCommand>> validators, CancellationToken cancellationToken)
//    {
//        if (!validators.Any())
//        {
//            return [];
//        }

//        var validationContext = new ValidationContext<TCommand>(command);

//        var results = await Task.WhenAll(
//            validators.Select(v => v.ValidateAsync(validationContext, cancellationToken))
//        );

//        var validationFailures = results.Where(r => !r.IsValid)
//            .SelectMany(r => r.AsErrors())
//            .ToList();

//        return validationFailures;
//    }

//}
