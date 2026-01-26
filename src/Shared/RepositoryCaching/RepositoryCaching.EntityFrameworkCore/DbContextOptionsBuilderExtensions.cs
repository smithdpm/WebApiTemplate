using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace RepositoryCaching.EntityFrameworkCore;
public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder AddCacheInvalidation(this DbContextOptionsBuilder optionsBuilder, IServiceProvider provider)
    {
        optionsBuilder.AddInterceptors(provider.GetRequiredService<CacheInvalidationInterceptor>());
        return optionsBuilder;
    }
}
