using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepositoryCaching.Cache;
using RepositoryCaching.Configuration;

namespace RepositoryCaching.EntityFrameworkCore;
public static class ServiceRegistrationExtensions
{
    public static void AddEFCoreCacheInvalidationServices(this IServiceCollection services,
       IConfiguration configuration)
    {
        var repositoryCachingSettings = configuration.GetSection(nameof(RepositoryCacheSettings))
            .Get<RepositoryCacheSettings>() ?? new RepositoryCacheSettings();

        if (repositoryCachingSettings.Enabled)
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCache>();

            services.AddCacheInvalidationServices(configuration);

            services.AddSingleton<CacheInvalidationInterceptor>();
        }
    }
}
