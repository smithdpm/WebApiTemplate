
using Cqrs.AzureServiceBus;
using Cqrs.Builders;
using Cqrs.EntityFrameworkCore;
using Cqrs.EntityFrameworkCore.Configuration;
using Cqrs.Outbox;
using Microsoft.EntityFrameworkCore;
using Product.App.Database;
using SharedKernel.Database;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureServices(builder.Configuration);
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var contexts = scope.ServiceProvider.GetServices<ApplicationDbContext>();
    foreach (var context in contexts)
    {
        context.Database.EnsureCreated();
        await DatabaseExtensions.SeedAsync(context);
    }
}

app.Run();

public partial class ProductAppProgram
{
}

public static class ConfigruationExtensions
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextFactory<ApplicationDbContext>((provider, options) =>
        {
            options
            .UseAzureSql(configuration.GetConnectionString("Database"))
            .AddCqrs(provider);
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork<ApplicationDbContext>>();

        var repositoryInterface = typeof(IRepository<>).MakeGenericType(typeof(OutboxMessage));
        var repositoryImplementation = typeof(AtomicRepository<>).MakeGenericType(typeof(OutboxMessage));
        services.AddScoped(repositoryInterface, repositoryImplementation);

        services.AddEventServices(configuration)
            .AddCqrsBehaviours(
                typeof(Program).Assembly,
                typeof(Program).Assembly,
                pipelineBuilder => { })
            .AddOutboxServices<ApplicationDbContext>(typeof(AtomicRepository<>), configuration);
    }
}
