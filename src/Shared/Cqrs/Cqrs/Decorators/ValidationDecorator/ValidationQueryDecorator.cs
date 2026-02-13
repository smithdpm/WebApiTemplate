using Ardalis.Result;
using Cqrs.Extensions;
using Cqrs.Messaging;
using FluentValidation;

namespace Cqrs.Decorators.ValidationDecorator;

public class ValidationQueryDecorator<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> innerHandler,
    IEnumerable<IValidator<TQuery>> validators
    ) : QueryHandlerDecorator<TQuery, TResponse>(innerHandler)
    where TQuery : IQuery<TResponse>
{

    public async override Task<Result<TResponse>> HandleAsync(TQuery input, CancellationToken cancellationToken)
    {
        var validationResult = await ValidationExtensions.ValidateAsync(input, validators, cancellationToken);
        if (validationResult.Any())
        {
            return Result<TResponse>.Invalid(validationResult);
        }

        return await HandleInner(input, cancellationToken);
    }
}
