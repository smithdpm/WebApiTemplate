﻿using Application.Abstractions.Events;
using Application.Abstractions.Services;
using Application.Behaviours.RepositoryCaching;
using Infrastructure.Authorization;
using Infrastructure.Database;
using Infrastructure.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using SharedKernel.Database;
using System.Reflection;

namespace Infrastructure;

public static class DependancyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration) => 
            services.AddDatabase(configuration)
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

            services.AddDbContext<CatalogContext>((provider, options) =>
            {
                var connection = provider.GetService<SqliteConnection>();
                options.UseSqlite(connection).UseSnakeCaseNamingConvention();
            });
        }
        else
        {
            string? connectionString = configuration.GetConnectionString("Database");

            services.AddDbContext<CatalogContext>(
                options => options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());
        }

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

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

    private static IServiceCollection RegisterEfRepositoryWithCacheInvalidationImplementations(this IServiceCollection services)
    {
        var assembly = typeof(Domain.IEntity<>).Assembly;
        var repositoryType = typeof(IRepository<>);
        var aggregateRootType = typeof(IAggregateRoot);
        var repositoryWithCacheInvalidationImplementationType = typeof(EfRepositoryWithCacheInvalidation<,>);

        var closedTypesOfIAggregateRoot = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && aggregateRootType.IsAssignableFrom(t));
        

        foreach (var aggregate in closedTypesOfIAggregateRoot)
        {
            var closedInterfaceType = repositoryType.MakeGenericType(aggregate);

            if (services.Any(s => s.ServiceType == closedInterfaceType))
                continue;

            if (aggregate.BaseType != null)
            {
                var idType = aggregate.BaseType.GenericTypeArguments[0];
                Type[] closedTypes = [aggregate, idType];

                var closedImplmentationType = repositoryWithCacheInvalidationImplementationType
                    .MakeGenericType(closedTypes);

                services.AddScoped(closedInterfaceType, closedImplmentationType);
            }            
        }

        return services;
    }

    private static IServiceCollection AddRepository(this IServiceCollection services,
        IConfiguration configuration)
    {
        var repositoryCachingSettings = configuration.GetSection(nameof(RepositoryCacheSettings))
            .Get<RepositoryCacheSettings>() ?? new RepositoryCacheSettings();
        services.Configure<RepositoryCacheSettings>(configuration.GetSection(nameof(RepositoryCacheSettings)));

        if (repositoryCachingSettings.Enabled)   
            return services.AddRepositoryWithCacheInvalidation(configuration);


        return services.AddRepositoryDefault(configuration);
    }

    private static IServiceCollection AddRepositoryDefault(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.ScanAssemblyAndRegisterClosedGenerics(typeof(Domain.IEntity<>).Assembly,
             typeof(IRepository<>), typeof(EfRepository<>), typeof(IAggregateRoot));

        services.ScanAssemblyAndRegisterClosedGenerics(typeof(Domain.IEntity<>).Assembly,
             typeof(IReadRepository<>), typeof(EfRepository<>), typeof(IAggregateRoot));

        return services;
    }

    private static IServiceCollection AddRepositoryWithCacheInvalidation(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.RegisterEfRepositoryWithCacheInvalidationImplementations();

        services.ScanAssemblyAndRegisterClosedGenerics(typeof(Domain.IEntity<>).Assembly,
             typeof(IReadRepository<>), typeof(EfRepository<>), typeof(IAggregateRoot));

        services.AddMemoryCache();
        services.AddScoped<ICacheService, MemoryCache>();

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
}
