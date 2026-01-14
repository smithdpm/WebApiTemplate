using Application.Behaviours;
using Application.Behaviours.Registries;
using Application.Behaviours.RepositoryCaching;
using Ardalis.Specification;
using Domain;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Abstractions;
using SharedKernel.Behaviours;
using SharedKernel.Database;
using SharedKernel.Events.DomainEvents;
using SharedKernel.Events.IntegrationEvents;
using SharedKernel.Messaging;
using System.Reflection;


namespace Application;

public static class DependancyInjection
{
    public static IServiceCollection AddInfrastructureDependantBehaviours(this IServiceCollection services, IConfiguration configuration)
    {
        var repositoryCachingSettings = configuration.GetSection(nameof(RepositoryCacheSettings))
            .Get<RepositoryCacheSettings>() ?? new RepositoryCacheSettings();

        if (repositoryCachingSettings.Enabled)
        {
            services.Decorate(typeof(IRepository<>), typeof(RepositoryCachingDecorator.CachedRepository<>));
            services.Decorate(typeof(IReadRepository<>), typeof(RepositoryCachingDecorator.CachedRepository<>));

            var invalidationMap = new InvalidationMap();
            
            services.AddSingleton<IInvalidationMap>(invalidationMap);

            services.Scan(scan => scan.FromAssembliesOf(typeof(DependancyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(IRepositoryCacheInvalidationHandler<>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

            services.Scan(scan => scan.FromAssembliesOf(typeof(DependancyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(ICacheInvalidationPolicy<>)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());
        }

        services.RegisterEntityFactories();

        return services;
    }


    public static IApplicationBuilder UseCacheInvalidationPolicies(this IApplicationBuilder app)
    {
        var scopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();

        using (var scope = scopeFactory.CreateScope())
        {
            var scopedProvider = scope.ServiceProvider;
            var invalidationMap = scopedProvider.GetRequiredService<IInvalidationMap>();

            if (invalidationMap is null)
                return app;

            var entityTypes = typeof(IEntity<>).Assembly
                    .GetTypes()
                    .Where(t => typeof(IHasId).IsAssignableFrom(t)
                    && !t.IsInterface && !t.IsAbstract);

            RegisterCachingInvalidationPoliciesForEntities(scopedProvider, invalidationMap, entityTypes);
        }

        return app;
    }

    private static IServiceCollection RegisterEntityFactories(this IServiceCollection services)
    {
        var factoryTypes = typeof(IEntityFactory<,>).Assembly
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

    private static void RegisterCachingInvalidationPoliciesForEntities(IServiceProvider provider, IInvalidationMap invalidationMap, IEnumerable<Type> entityTypes)
    {
        MethodInfo helper = GetPolicyRegistrationHelperMethod();

        foreach (var entityType in entityTypes)
        {
            var entityInterface = entityType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));

            if (entityInterface is null)
                continue;

            helper.MakeGenericMethod(entityType).Invoke(null, new object[] { invalidationMap, provider });
        }
    }

    private static MethodInfo GetPolicyRegistrationHelperMethod()
    {
        return typeof(DependancyInjection)
                .GetMethod(nameof(RegisterPoliciesForType), BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new InvalidOperationException("Helper method not found.");
    }

    private static void RegisterPoliciesForType<TEntity>(IInvalidationMap map, IServiceProvider provider)
        where TEntity : IHasId
    {
        var policies = provider.GetServices<ICacheInvalidationPolicy<TEntity>>();

        if (!policies.Any()) return;

        Func<ChangedEntity<TEntity>, IEnumerable<string>> combinedFunc = changedEntity =>
            policies.SelectMany(p => p.GetKeysToInvalidate(changedEntity));

        map.RegisterMap(combinedFunc);
    }
}
