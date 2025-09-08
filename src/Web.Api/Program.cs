using Application;
using SharedKernel.Messaging;
using Application.Cars;
using Application.Cars.Create;
using Application.Cars.Get;
using Infrastructure;
using Infrastructure.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.Resource;
using System.Reflection;
using Web.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
//builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi()
//    .AddInfrastructure(builder.Configuration)
//    .AddApplication();

builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();


builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());
//builder.Services.AddInfrastructureDependantBehaviours();

var app = builder.Build();

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


var scopeRequiredByApi = app.Configuration["AzureAd:Scopes"] ?? "";
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (HttpContext httpContext) =>
{
    //httpContext.VerifyUserHasAnyAcceptedScope(scopeRequiredByApi);

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();
//.RequireAuthorization();

app.UseAuthentication();
app.UseAuthorization();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
