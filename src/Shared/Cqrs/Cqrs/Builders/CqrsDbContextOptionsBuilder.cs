

using Cqrs.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.Builders;
public static class CqrsDbContextOptionsBuilder
{
    public static DbContextOptionsBuilder AddCqrs(this DbContextOptionsBuilder optionsBuilder, IServiceProvider provider)
    {
        optionsBuilder.AddInterceptors(provider.GetRequiredService<OutboxSaveChangesInterceptor>());
        return optionsBuilder;
    }
}
