using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;
using ReprEndpoints.Endpoints;
using System.Reflection;

namespace ReprEndpoints.Extensions;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services)
    {
        List<Assembly> assemblies = GetDependantAssemblies();

        foreach (var assembly in assemblies)
        {
            var endpointDescriptions = GetTypesSafely(assembly!)
            .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                t.IsAssignableTo(typeof(IEndpoint)))
            .ToList();

            foreach (var type in endpointDescriptions)
            {
                services.TryAddEnumerable(ServiceDescriptor.Transient(typeof(IEndpoint), type));
            }
        }

        return services;
    }

    private static List<Assembly> GetDependantAssemblies()
    {
        var interfaceType = typeof(IEndpoint);
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

    private static IEnumerable<Type> GetTypesSafely(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
        catch
        {
            return Enumerable.Empty<Type>();
        }
    }
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.ServiceProvider.GetServices<IEndpoint>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }
        return app;
    }
}
