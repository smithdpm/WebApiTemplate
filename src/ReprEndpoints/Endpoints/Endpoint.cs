using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ReprEndpoints.Endpoints.Filters;

namespace ReprEndpoints.Endpoints;

public abstract partial class Endpoint : IEndpoint
{
    protected abstract string EndpointPath { get; }
    protected abstract Delegate Handler { get; }
    protected abstract HttpVerb HttpVerb { get; }

    private readonly List<string> _tags = new();
    private string _name = "";
    private string? _permission;

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        Configure();
        var builder = MapVerb(app);
        AddEndpointFilter(builder);
        Produces(builder);
        ConfigureEndpoint(builder);
    }

    public void AddTag(string tag)
    {
        _tags.Add(tag);
    }

    public void Name(string name)
    {
        _name = name;
    }

    public void RequirePermission(string permission)
    {
        _permission = permission;
    }

    protected virtual void Produces(RouteHandlerBuilder builder) { }

    protected virtual void AddEndpointFilter(RouteHandlerBuilder builder) { }

    protected virtual void ConfigureEndpoint(RouteHandlerBuilder app) 
    { 
        if (!string.IsNullOrWhiteSpace(_name))
        {
            app.WithName(_name);
        }
        if (_tags.Any())
        {
            app.WithTags(_tags.ToArray());
        }
        if (!string.IsNullOrWhiteSpace(_permission))
        {
            app.RequireAuthorization(_permission);
        }
        app.WithOpenApi();
    }

    protected virtual void Configure() { }

    private RouteHandlerBuilder MapVerb(IEndpointRouteBuilder app)
    {
        switch (HttpVerb)
        {
            case HttpVerb.GET:
                return app.MapGet(EndpointPath, Handler);
            case HttpVerb.POST:
                return app.MapPost(EndpointPath, Handler);
            case HttpVerb.PUT:
                return app.MapPut(EndpointPath, Handler);
            case HttpVerb.DELETE:
                return app.MapDelete(EndpointPath, Handler);
            default:
                throw new NotSupportedException($"HTTP method '{HttpVerb}' is not supported.");
        }
    }
}

public abstract partial class Endpoint<TRequest> : Endpoint
    where TRequest : notnull
{
    protected override void ConfigureEndpoint(RouteHandlerBuilder app)
    {
        base.ConfigureEndpoint(app);
        app.AddEndpointFilter<ValidationEndpointFilter<TRequest>>();
    }
}
public abstract partial class Endpoint<TRequest, TResponse> : Endpoint
    where TRequest : notnull
{
    protected override void Produces(RouteHandlerBuilder builder)
    {
        builder.Produces<TResponse>(StatusCodes.Status200OK);
        builder.ProducesValidationProblem();
        builder.Produces(StatusCodes.Status204NoContent);
    }
    protected override void ConfigureEndpoint(RouteHandlerBuilder app)
    {
        base.ConfigureEndpoint(app);
        app.AddEndpointFilter<ValidationEndpointFilter<TRequest>>();
    }
}
public abstract partial class NoRequestEndpoint<TResponse> : Endpoint
{
    protected override void Produces(RouteHandlerBuilder builder)
    {
        builder.Produces<TResponse>(StatusCodes.Status200OK);
        builder.ProducesValidationProblem();
        builder.Produces(StatusCodes.Status204NoContent);
    }
}
