
using Azure.Messaging.ServiceBus;
using Cqrs.Abstractions.Events;
using Cqrs.AzureServiceBus.Dispatcher;
using Cqrs.AzureServiceBus.Reciever.Subscriber;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cqrs.AzureServiceBus;

public static class DependancyInjection
{
    public static IServiceCollection AddEventServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAzureServiceBus(configuration);
        services.AddScoped<IIntegrationEventDispatcher, ServiceBusEventDispatcher>();   
        return services;
    }

    private static IServiceCollection AddAzureServiceBus(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceBusEnabled = false;
        bool.TryParse(configuration.GetSection("CqrsSettings:AzureServiceBus")["Enabled"], out serviceBusEnabled);

        if (!serviceBusEnabled)
            return services;

        var connectionString = configuration.GetSection("CqrsSettings:AzureServiceBus")["ConnectionString"];
        services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClient(connectionString);
        });

        services.AddAzureServiceBusSenders(configuration);
        services.AddAzureServiceBusTopicSubscribers(configuration);

        return services;
    }

    private static IServiceCollection AddAzureServiceBusSenders(this IServiceCollection services, IConfiguration configuration)
    {
        var queueNames = configuration.GetSection("CqrsSettings:DispatchSettings:Queues").Get<List<string>>();
        var topicNames = configuration.GetSection("CqrsSettings:DispatchSettings:Topics").Get<List<string>>();

        var queueAndTopicNames = new List<string>();
        if (queueNames != null)
            queueAndTopicNames.AddRange(queueNames);
        if (topicNames != null)
            queueAndTopicNames.AddRange(topicNames);

        services.AddAzureClients(builder =>
        {
            foreach (var name in queueAndTopicNames)
            {
                builder.AddClient<ServiceBusSender, ServiceBusClientOptions>((_, _, provider) =>
                    provider
                        .GetRequiredService<ServiceBusClient>()
                        .CreateSender(name)
                )
                .WithName(name);
            }
        });

        return services;
    }

    private static IServiceCollection AddAzureServiceBusTopicSubscribers(this IServiceCollection services, IConfiguration configuration)
    {
        var topicSubscribers = configuration.GetSection("CqrsSettings:Recievers:TopicSubscribers").Get<List<TopicSubscriberSettings>>();

        if (topicSubscribers == null)
            return services;

        foreach (var topicSubscriber in topicSubscribers)
        {
            services.AddHostedService(provider =>
            ActivatorUtilities.CreateInstance<ServiceBusWorker>(
                    provider,
                    topicSubscriber)
            );
        }
        return services;
    }
}