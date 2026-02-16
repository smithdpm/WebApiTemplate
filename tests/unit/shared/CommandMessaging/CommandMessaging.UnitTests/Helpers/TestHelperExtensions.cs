

using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.UnitTests.Helpers;
public static class TestHelperExtensions
{
    public static void ShouldContainService<TService, TImplementation>(this IServiceCollection services, ServiceLifetime lifetime)
    {
        var exists = services.Any(d => 
            d.ServiceType == typeof(TService) && 
            d.ImplementationType == typeof(TImplementation) && 
            d.Lifetime == lifetime);
        Assert.True(exists, $"Service {typeof(TService).Name} not registered as {lifetime}");
    }
}

