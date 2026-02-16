using Cqrs.EntityFrameworkCore.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.EntityFrameworkCore.Configuration;
public static class CommandMessagingDbContextOptionsBuilder
{
    public static DbContextOptionsBuilder AddCqrs(this DbContextOptionsBuilder optionsBuilder, IServiceProvider provider)
    {
        optionsBuilder.AddInterceptors(provider.GetRequiredService<OutboxSaveChangesInterceptor>());
        return optionsBuilder;
    }
}
