using Application.Abstractions.Events;
using Application.Abstractions.Services;
using Application.Behaviours.RepositoryCaching;
using Azure.Messaging.ServiceBus;
using Domain.Abstractions;
using Infrastructure.Authorization;
using Infrastructure.Database;
using Infrastructure.Events;
using Infrastructure.Events.ServiceBus;
using Infrastructure.IdentityGeneration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using SharedKernel.Abstractions;
using SharedKernel.Behaviours;
using SharedKernel.Database;
using SharedKernel.Events;
using SharedKernel.Messaging;
using System.Reflection;

namespace Infrastructure;

public static class DependancyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration) => 
            services.AddDatabase(configuration)           
                .AddAzureServiceBus(configuration)
                .AddOutboxServices<ApplicationContext>()
                .AddIdentityGenerators()
                .AddAuthenticationCustom(configuration)
                .AddAuthorizationCustom();


    private static IServiceCollection AddDatabase(this IServiceCollection services, 
        IConfiguration configuration)
    {
        bool useOnlyInMemoryDatabase = false;
        if (configuration["UseOnlyInMemoryDatabase"] != null)
        {
            useOnlyInMemoryDatabase = bool.Parse(configuration["UseOnlyInMemoryDatabase"]!);
        }

        if (useOnlyInMemoryDatabase)
        {

            services.AddSingleton(sp =>
            {
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open(); // Keep the connection open for the lifetime of the app
                return connection;
            });

            services.AddDbContextFactory<ApplicationContext>((provider, options) =>
            {
                var connection = provider.GetService<SqliteConnection>();
                options.UseSqlite(connection);
            });
        }
        else
        {
            string? connectionString = configuration.GetConnectionString("Database");
            services.AddSingleton<CacheInvalidatorGenericHandler>();
            services.AddSingleton<CacheInvalidationInterceptor>();
            services.AddSingleton<OutboxSaveChangesInterceptor>();
            services.AddDbContextFactory<ApplicationContext>((provider, options) =>
            {
                options
                .UseAzureSql(connectionString)
                .AddInterceptors(provider.GetRequiredService<OutboxSaveChangesInterceptor>(), provider.GetRequiredService<CacheInvalidationInterceptor>());
            });
        }


        services.AddRepository(configuration);

        return services;
    }

    private static IServiceCollection ScanAssemblyAndRegisterClosedGenerics(this IServiceCollection services, Assembly assembly
        , Type interfaceType, Type implementationType, Type openGenericType)
    {
        var closedTypesOfOpenGeneric = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && openGenericType.IsAssignableFrom(t));

        foreach (var closedType in closedTypesOfOpenGeneric)
        {
            var closedInterfaceType = interfaceType.MakeGenericType(closedType);
            var closedImplmentationType = implementationType.MakeGenericType(closedType);

            if (!services.Any(s => s.ServiceType == closedInterfaceType))
                services.AddScoped(closedInterfaceType, closedImplmentationType);
        }

        return services;
    }

    private static IServiceCollection AddRepository(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, EfUnitOfWork<ApplicationContext>>();

        var repositoryCachingSettings = configuration.GetSection(nameof(RepositoryCacheSettings))
            .Get<RepositoryCacheSettings>() ?? new RepositoryCacheSettings();
        services.Configure<RepositoryCacheSettings>(configuration.GetSection(nameof(RepositoryCacheSettings)));

        services.ScanAssemblyAndRegisterClosedGenerics(typeof(Domain.IEntity<>).Assembly,
            typeof(IRepository<>), typeof(EfRepository<>), typeof(IAggregateRoot));

        services.ScanAssemblyAndRegisterClosedGenerics(typeof(Domain.IEntity<>).Assembly,
             typeof(IReadRepository<>), typeof(EfRepository<>), typeof(IAggregateRoot));

        if (repositoryCachingSettings.Enabled)
        {
            services.AddMemoryCache();
            services.AddScoped<ICacheService, MemoryCache>();
        }

        return services;
    }


    private static IServiceCollection AddAzureServiceBus(this IServiceCollection services, IConfiguration configuration)
    {
        var serviceBusEnabled = false;
        bool.TryParse(configuration.GetSection("AzureServiceBus")["Enabled"], out serviceBusEnabled);

        if (!serviceBusEnabled)
            return services;


        var connectionString = configuration.GetSection("AzureServiceBus")["ConnectionString"];
        services.AddAzureClients(builder =>
        {
            builder.AddServiceBusClient(connectionString);
        });

        services.AddAzureServiceBusQueueSenders(configuration);
        services.AddAzureServiceBusTopicSubscribers(configuration);

        return services;
    }

    private static IServiceCollection AddAzureServiceBusQueueSenders(this IServiceCollection services, IConfiguration configuration)
    {
        var queueNames = configuration.GetSection("AzureServiceBus:Sender:Queues").Get<List<string>>();
        var topicNames = configuration.GetSection("AzureServiceBus:Sender:Topics").Get<List<string>>();

        var queueAndTopicNames = new List<string>();
        if (queueNames!=null)
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
        var topicSubscribers = configuration.GetSection("AzureServiceBus:TopicSubscribers").Get<List<ServiceBusTopicSubscriberSettings>>();

        if (topicSubscribers == null)
            return services;

        foreach (var topicSubscriber in topicSubscribers)
        {
            services.AddHostedService(provider =>
            ActivatorUtilities.CreateInstance<ServiceBusTopicSubscriber>(
                    provider,
                    topicSubscriber)
            );
        }
        return services;
    }

    private static IServiceCollection AddAuthenticationCustom(this IServiceCollection services, IConfiguration configuration) 
    {   
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
           .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        return services;
    } 


    private static IServiceCollection AddAuthorizationCustom(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();  
        services.AddTransient<IAuthorizationHandler, PermissionAutherizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }

    private static IServiceCollection AddIdentityGenerators(this IServiceCollection services)
    {
        services.AddScoped<IIdGenerator<Guid>, UuidSqlServerFriendlyGenerator>();

        return services;
    }

    private static IServiceCollection AddOutboxServices<TContext>(this IServiceCollection services) 
        where TContext : DbContext
    {
        services.AddScoped<IRepository<OutboxMessage>, EfRepository<OutboxMessage>>();
        services.AddSingleton<IOutboxRepository, OutboxRepository<TContext>>();
        services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<IIntegrationEventDispatcher, ServiceBusEventDispatcher>();
        services.AddHostedService(provider=>
            ActivatorUtilities.CreateInstance<OutboxDispatcher>(provider));

        return services;
    }
}




