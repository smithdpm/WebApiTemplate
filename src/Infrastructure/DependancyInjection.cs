using Application.Behaviours;
using Infrastructure.Authorization;
using Infrastructure.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using SharedKernel.Database;

namespace Infrastructure;

public static class DependancyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
        IConfiguration configuration) => 
            services.AddDatabase(configuration)
                .AddCaching(configuration)
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

            //var provider = services.BuildServiceProvider();
            //var connection = provider.GetService<SqliteConnection>();

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

        var repositoryType = typeof(IRepository<>);
        System.Diagnostics.Debug.WriteLine($"INFRASTRUCTURE is registering type: {repositoryType.AssemblyQualifiedName}");

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

        services.Decorate(typeof(IRepository<>), typeof(CachingDecorator.CachedRepository<>));

        return services;
    }

    private static IServiceCollection AddCaching(this IServiceCollection services,
        IConfiguration configuration)
    {
        bool useInMemoryCache = false;
        if (configuration["UseInMemoryCache"] != null)
        {
            useInMemoryCache = bool.Parse(configuration["UseInMemoryCache"]!);
        }

        //if (useInMemoryCache)
        //{
        services.AddMemoryCache();
        services.AddScoped<Application.Services.ICacheService, MemoryCache>();
        //}

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
