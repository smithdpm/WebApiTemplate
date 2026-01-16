using Application.Abstractions.Services;
using Application.Behaviours.RepositoryCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Database;

namespace Infrastructure.Database;
public static class CacheInvalidationDbContextOptionsBuilder
{
    //public static void ConfigureCacheInvalidation(this DbContextOptionsBuilder optionsBuilder, IServiceProvider provider)
    //{
    //    var cacheInvalidationInterceptor = provider.GetRequiredService<CacheInvalidationInterceptor>();
    //    optionsBuilder.AddInterceptors(cacheInvalidationInterceptor);
    //}

    public static void AddCacheInvalidationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var repositoryCachingSettings = configuration.GetSection(nameof(RepositoryCacheSettings))
            .Get<RepositoryCacheSettings>() ?? new RepositoryCacheSettings();

        if (repositoryCachingSettings.Enabled)
        {
            services.Decorate(typeof(IRepository<>), typeof(RepositoryCachingDecorator.CachedRepository<>));
            services.Decorate(typeof(IReadRepository<>), typeof(RepositoryCachingDecorator.CachedRepository<>));

            var invalidationMap = new InvalidationMap();

            services.AddSingleton<IInvalidationMap>(invalidationMap);

            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCache>();

            services.Scan(scan => scan.FromAssembliesOf(typeof(DependancyInjection))
            .AddClasses(classes => classes.AssignableTo(typeof(ICacheInvalidationPolicy)), false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());
            services.AddSingleton<IRepositoryCacheInvalidationHandler, RepositoryCacheInvalidationHandler>();
            services.AddSingleton<CacheInvalidationInterceptor>();
        }      
    }
    public static DbContextOptionsBuilder AddCacheInvalidation(this DbContextOptionsBuilder optionsBuilder, IServiceProvider provider)
    {
        optionsBuilder.AddInterceptors(provider.GetRequiredService<CacheInvalidationInterceptor>());
        return optionsBuilder;
    }
}
