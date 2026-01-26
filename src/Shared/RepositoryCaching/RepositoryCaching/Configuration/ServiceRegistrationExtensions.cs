using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using RepositoryCaching.Database;
using RepositoryCaching.Invalidation.Handlers;
using RepositoryCaching.Invalidation.Maps;
using RepositoryCaching.Invalidation.Policies;
using SharedKernel.Database;
using System.ComponentModel;
using System.Reflection;

namespace RepositoryCaching.Configuration;
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ServiceRegistrationExtensions
{
    public static void AddCacheInvalidationServices(this IServiceCollection services,
       IConfiguration configuration)
    {
        services.Decorate(typeof(IRepository<>), typeof(RepositoryCachingDecorator.CachedRepository<>));
        services.Decorate(typeof(IReadRepository<>), typeof(RepositoryCachingDecorator.CachedRepository<>));

        services.RegisterInvalidationPolicies();

        services.AddSingleton<IInvalidationMap, InvalidationMap>();
        services.AddSingleton<IRepositoryCacheInvalidationHandler, RepositoryCacheInvalidationHandler>();
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
