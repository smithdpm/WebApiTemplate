

using Application.Behaviours.Registries;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Behaviours;
using SharedKernel.Events.DomainEvents;
using SharedKernel.Events.IntegrationEvents;
using SharedKernel.Messaging;
using System.Reflection;

namespace Application.Behaviours;
public static class CommandRegistrationExtension
{
    public static IServiceCollection AddCqrsBehaviours(this IServiceCollection services,
        Assembly applicationAssembly,
        Assembly domainAssembly,
        Action<ICommandPipelineBuilder> configurePipeline)
    {
        
        services.Scan(scan => scan.FromAssemblies(applicationAssembly)
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IIntegrationEventHandler<>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IDomainEventHandler<>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.AddValidatorsFromAssembly(typeof(DependancyInjection).Assembly, includeInternalTypes: true);

        services.AddSingleton<IEventTypeRegistry>(r =>
        {
            var registry = new EventTypeRegistry();
            registry.RegisterDomainEventsFromAssemblyTypes(domainAssembly.GetTypes());
            registry.RegisterIntegrationEventsFromAssemblyTypes(applicationAssembly.GetTypes());
            return registry;
        });

        var builder = new CommandPipelineBuilder(services);
        configurePipeline(builder);

        return services;
    }
}
