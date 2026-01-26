using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace ReprEndpoints.Endpoints.Filters;
public class ValidationEndpointFilter<TRequest>(IEnumerable<IValidator<TRequest>> validators) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (!validators.Any())
        {
            return await next(context);
        }

        var request = context.Arguments
            .OfType<TRequest>()
            .FirstOrDefault();

        if (request is null)
        {
            return Results.Problem(
                statusCode: 500,
                title: "Validation Error",
                detail: $"Could not find argument of type {typeof(TRequest).Name} to validate.");
        }

        var contextValidation = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(contextValidation))
        );

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            return TypedResults.ValidationProblem(
                failures.ToDictionary(
                    x => x.PropertyName,
                    x => new[] { x.ErrorMessage }
                ));
        }

        return await next(context);
    }
}
