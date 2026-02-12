
using Cqrs.Abstractions.Events;
using Cqrs.Decorators.AtomicTransactionDecorator;
using Cqrs.Decorators.IntegrationEventToOutboxDecorator;
using Cqrs.Decorators.LoggingDecorator;
using Cqrs.Decorators.Registries;
using Cqrs.Events.DomainEvents;
using Cqrs.Events.IntegrationEvents;
using Cqrs.MessageBroker;
using Cqrs.Messaging;
using Cqrs.Outbox;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Cqrs.Builders;
public static class CommandRegistrationExtension
{
    public static IServiceCollection AddCqrsBehaviours(this IServiceCollection services,
        Assembly applicationAssembly,
        Assembly domainAssembly,
        Action<ICommandPipelineBuilder> configurePipeline)
    {
        services.AddCqrsBehaviours(applicationAssembly, domainAssembly);
        services.ConfigurePipeline(configurePipeline);
        return services;
    }
    private static IServiceCollection AddCqrsBehaviours(this IServiceCollection services,
        Assembly applicationAssembly,
        Assembly domainAssembly)
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

        services.AddScoped<ILoggingBehaviour, LoggingBehaviour>();
        services.AddScoped<IAtomicTransactionBehaviour, AtomicTransactionBehaviour>();
        services.AddScoped<IIntegrationEventToOutboxBehaviour, IntegrationEventToOutboxBehaviour>();

        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.AddSingleton<IEventTypeRegistry>(r =>
        {
            var registry = new EventTypeRegistry();
            registry.RegisterDomainEventsFromAssemblyTypes(domainAssembly.GetTypes());
            registry.RegisterIntegrationEventsFromAssemblyTypes(applicationAssembly.GetTypes());
            return registry;
        });

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IOutboxDispatcher, OutboxDispatcher>();
        services.AddScoped<IMessageHandler, IntegreationEventMessageHandler>();

        return services;
    }

    private static IServiceCollection ConfigurePipeline(this IServiceCollection services, Action<ICommandPipelineBuilder> configurePipeline)
    {
        var builder = new CommandPipelineBuilder(services);
        builder.AddIntegrationEventHandling();
        builder.AddAtomicTransactionHandling();
        configurePipeline(builder);
        builder.AddValidation();
        builder.AddLogging();

        return services;
    }
}
