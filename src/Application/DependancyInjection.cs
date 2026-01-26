using Ardalis.Specification;
using Domain;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions;


namespace Application;

public static class DependancyInjection
{
    public static IServiceCollection AddInfrastructureDependantBehaviours(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterEntityFactories();

        return services;
    }

    private static IServiceCollection RegisterEntityFactories(this IServiceCollection services)
    {
        var factoryTypes = typeof(Entity<>).Assembly
            .GetTypes()
            .Where(t => t.IsInterface && t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityFactory<,>)));

        foreach (var factoryType in factoryTypes)
        {
            services.Scan(scan => scan.FromAssembliesOf(typeof(Entity<>))
            .AddClasses(classes => classes.AssignableTo(factoryType), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }
        return services;
    }
}
