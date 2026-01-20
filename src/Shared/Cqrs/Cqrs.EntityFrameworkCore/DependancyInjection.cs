using Cqrs.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Database;
using Cqrs.EntityFrameworkCore.Database;

namespace Cqrs.EntityFrameworkCore;
public static class DependancyInjection
{
    public static IServiceCollection AddOutboxServices<TContext>(this IServiceCollection services, Type repositoryImplementation)
    where TContext : DbContext
    {
        services.AddRepository(repositoryImplementation);
        services.AddSingleton<IOutboxRepository, OutboxRepository<TContext>>();
        services.AddSingleton<OutboxSaveChangesInterceptor>();
        services.AddHostedService(provider =>
            ActivatorUtilities.CreateInstance<OutboxDispatcher>(provider));

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
