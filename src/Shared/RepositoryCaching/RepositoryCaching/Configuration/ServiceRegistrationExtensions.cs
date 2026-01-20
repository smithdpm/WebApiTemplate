using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
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

            services.RegisterInvalidationPolicies();

            services.AddSingleton<IInvalidationMap, InvalidationMap>();
            services.AddSingleton<IRepositoryCacheInvalidationHandler, RepositoryCacheInvalidationHandler>();
            services.AddSingleton<CacheInvalidationInterceptor>();
        }
    }

    private static void RegisterInvalidationPolicies(this IServiceCollection services)
    {
        services.Scan(scan => scan.FromAssemblies(GetDependantAssemblies())
            .AddClasses(classes => classes.AssignableTo(typeof(ICacheInvalidationPolicy)), false)
                .AsImplementedInterfaces()
                .WithSingletonLifetime());
    }
    private static IEnumerable<Assembly> GetDependantAssemblies()
    {
        var interfaceType = typeof(ICacheInvalidationPolicy);
        var interfaceAssemblyName = interfaceType.Assembly.GetName().Name;

        var assemblies = DependencyContext.Default!.RuntimeLibraries
            .Where(lib =>
                lib.Dependencies.Any(d => d.Name == interfaceAssemblyName))
            .Select(lib =>
            {
                try { return Assembly.Load(new AssemblyName(lib.Name)); }
                catch { return null; }
            })
            .Where(a => a is not null)
            .ToList();
        return assemblies!;
    }
}
