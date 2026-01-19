using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryCaching.Cache;
using RepositoryCaching.Database;
using RepositoryCaching.Invalidation.Handlers;
using RepositoryCaching.Invalidation.Maps;
using RepositoryCaching.Invalidation.Policies;
using SharedKernel.Database;
using System.Reflection;

namespace RepositoryCaching.Configuration;

public static class ServiceRegistrationExtensions
{
    public static void AddCacheInvalidationServices(this IServiceCollection services,
       Assembly[] assembliesWithInvalidationPolicies,
       IConfiguration configuration)
    {
        var repositoryCachingSettings = configuration.GetSection(nameof(RepositoryCacheSettings))
            .Get<RepositoryCacheSettings>() ?? new RepositoryCacheSettings();

        if (repositoryCachingSettings.Enabled)
        {
            services.Decorate(typeof(IRepository<>), typeof(RepositoryCachingDecorator.CachedRepository<>));
            services.Decorate(typeof(IReadRepository<>), typeof(RepositoryCachingDecorator.CachedRepository<>));

            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCache>();

            services.Scan(scan => scan.FromAssemblies(assembliesWithInvalidationPolicies)
            .AddClasses(classes => classes.AssignableTo(typeof(ICacheInvalidationPolicy)), false)
                .AsImplementedInterfaces()
                .WithSingletonLifetime());

            services.AddSingleton<IInvalidationMap, InvalidationMap>();
            services.AddSingleton<IRepositoryCacheInvalidationHandler, RepositoryCacheInvalidationHandler>();
            services.AddSingleton<CacheInvalidationInterceptor>();
        }
    }
}
