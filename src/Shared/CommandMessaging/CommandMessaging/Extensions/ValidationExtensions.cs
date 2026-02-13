using Ardalis.Result;
using Ardalis.Result.FluentValidation;
using FluentValidation;

namespace Cqrs.Extensions;

public static class ValidationExtensions
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
