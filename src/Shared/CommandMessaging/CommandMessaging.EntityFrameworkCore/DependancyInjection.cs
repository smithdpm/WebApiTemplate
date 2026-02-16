using Cqrs.BackgroundServices;
using Cqrs.EntityFrameworkCore.Database;
using Cqrs.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Database;

namespace Cqrs.EntityFrameworkCore;
public static class DependancyInjection
{
    public static IServiceCollection AddOutboxServices<TContext>(this IServiceCollection services, Type repositoryImplementation, IConfiguration configuration)
    where TContext : DbContext
    {
        services.Configure<OutboxConfigurationSettings>(configuration.GetSection("CqrsSettings:OutboxConfiguration"));

        services.AddRepository(repositoryImplementation);
        services.AddScoped<IOutboxRepository, OutboxRepository<TContext>>();
        services.AddSingleton<OutboxSaveChangesInterceptor>();
        services.AddHostedService<OutboxDispatcherService>();

        return services;
    }

    private static IServiceCollection AddRepository(this IServiceCollection services,
        Type repositoryImplementation)
    {
        if (!IsValidRepositoryType(repositoryImplementation))
            throw new Exception("The Type repositoryImplementation must be of type IRepository<>.");

        var outboxRepositoryType = repositoryImplementation.MakeGenericType(typeof(OutboxMessage));
        services.AddScoped(typeof(IRepository<OutboxMessage>), outboxRepositoryType);

        return services;
    }

    private static bool IsValidRepositoryType(Type repositoryImplementation)
    {
        return repositoryImplementation.IsGenericTypeDefinition &&
               repositoryImplementation.GetInterfaces()
                   .Any(i => i.IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IRepository<>));
    }

}
