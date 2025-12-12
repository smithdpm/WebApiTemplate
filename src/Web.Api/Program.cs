using Application;
using Infrastructure;
using Infrastructure.Database;
using System.Reflection;
using Web.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
//builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi()
   .AddApplication()
   .AddInfrastructure(builder.Configuration);
   

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());
builder.Services.AddInfrastructureDependantBehaviours(builder.Configuration);


var app = builder.Build();
app.UseCacheInvalidationPolicies();
app.MapEndpoints();

using (var scope = app.Services.CreateScope())
{
   var contexts = scope.ServiceProvider.GetServices<CatalogContext>();
   foreach (var context in contexts)
    {
        context.Database.EnsureCreated();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

public partial class Program 
{
    // Expose the Program class for use with WebApplicationFactory<T>
}