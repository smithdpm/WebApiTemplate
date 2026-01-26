using Cqrs.EntityFrameworkCore.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.EntityFrameworkCore.Configuration;
public static class CqrsDbContextOptionsBuilder
{
    public static DbContextOptionsBuilder AddCqrs(this DbContextOptionsBuilder optionsBuilder, IServiceProvider provider)
    {
        optionsBuilder.AddInterceptors(provider.GetRequiredService<OutboxSaveChangesInterceptor>());
        return optionsBuilder;
    }
}
