namespace Web.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder AddRequestIdHeader(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("x-request-id", Guid.CreateVersion7().ToString());

            await next();
        });
    }
}
