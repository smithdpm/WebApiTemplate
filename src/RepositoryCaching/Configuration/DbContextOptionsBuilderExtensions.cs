using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RepositoryCaching.Database;

namespace RepositoryCaching.Configuration;
public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder AddCacheInvalidation(this DbContextOptionsBuilder optionsBuilder, IServiceProvider provider)
    {
        optionsBuilder.AddInterceptors(provider.GetRequiredService<CacheInvalidationInterceptor>());
        return optionsBuilder;
    }
}
