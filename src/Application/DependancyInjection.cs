using SharedKernel.Behaviours;
using SharedKernel.Messaging;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Database;
using Application.Behaviours.RepositoryCaching;


namespace Application;

public static class DependancyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        
        services.Scan(scan => scan.FromAssembliesOf(typeof(DependancyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.AddValidatorsFromAssembly(typeof(DependancyInjection).Assembly, includeInternalTypes: true);

        services.Decorate(typeof(ICommandHandler<,>), typeof(LoggingDecorator.CommandHandler<,>));
        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationDecorator.CommandHandler<,>));

        return services;          
    }

    public static IServiceCollection AddInfrastructureDependantBehaviours(this IServiceCollection services, IConfiguration configuration)
    {
        var repositoryCachingSettings = configuration.GetSection(nameof(RepositoryCacheSettings))
            .Get<RepositoryCacheSettings>() ?? new RepositoryCacheSettings();

        if (repositoryCachingSettings.Enabled)
        {
            services.Decorate(typeof(IRepository<>), typeof(RepositoryCachingDecorator.CachedRepository<>));
            services.Decorate(typeof(IReadRepository<>), typeof(RepositoryCachingDecorator.CachedRepository<>));
        }
                  
        return services;
    }

}
