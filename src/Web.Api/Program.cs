using Application;
using Cqrs;
using Cqrs.Builders;
using Cqrs.Database;
using Infrastructure;
using Infrastructure.Database;
using Scalar.AspNetCore;
using SharedKernel.Database;
using System.Reflection;
using Web.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
//builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi()
   .AddInfrastructure(builder.Configuration)
   .AddCqrsBehaviours(
        typeof(Application.DependancyInjection).Assembly,
        typeof(Domain.Entity<>).Assembly,
        pipelineBuilder =>
        {
            pipelineBuilder.AddIntegrationEventHandling();
            pipelineBuilder.AddAtomicTransactionHandling();
            pipelineBuilder.AddValidation();
            pipelineBuilder.AddLogging();
        })
   .AddOutboxServices<ApplicationContext>()
   .AddScoped<IRepository<OutboxMessage>, EfRepository<OutboxMessage>>(); ;


builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());
builder.Services.AddInfrastructureDependantBehaviours(builder.Configuration);

var app = builder.Build();
app.UseCacheInvalidationPolicies();
app.MapEndpoints();


using (var scope = app.Services.CreateScope())
{
   var contexts = scope.ServiceProvider.GetServices<ApplicationContext>();
   foreach (var context in contexts)
    {
        context.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });

    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.AddRequestIdHeader();
app.UseAuthentication();
app.UseAuthorization();

app.Run();

public partial class Program 
{
    // Expose the Program class for use with WebApplicationFactory<T>
}