using Application.Abstractions.Database;
using Domain.Cars;
using Infrastructure.Database;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependancyInjection
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services,
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

            var provider = services.BuildServiceProvider();
            var connection = provider.GetService<SqliteConnection>();

            services.AddDbContext<CatalogContext>(options => options
            .UseSqlite(connection)
            .UseSnakeCaseNamingConvention());
        }
        else
        {
            string? connectionString = configuration.GetConnectionString("Database");

            services.AddDbContext<CatalogContext>(
                options => options
                .UseNpgsql(connectionString)
                .UseSnakeCaseNamingConvention());
        }
        
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));

        return services;

    }

}
