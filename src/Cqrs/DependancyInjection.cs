

using Cqrs.Abstractions.Events;
using Cqrs.Database;
using Cqrs.Events;
using Cqrs.Events.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Database;

namespace Cqrs;
public static class DependancyInjection
{
    public static IServiceCollection AddOutboxServices<TContext>(this IServiceCollection services)
    where TContext : DbContext
    {
        //services.AddScoped<IRepository<OutboxMessage>, EfRepository<OutboxMessage>>();
        services.AddSingleton<IOutboxRepository, OutboxRepository<TContext>>();
        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<IIntegrationEventDispatcher, ServiceBusEventDispatcher>();
        services.AddHostedService(provider =>
            ActivatorUtilities.CreateInstance<OutboxDispatcher>(provider));

        return services;
    }
}
