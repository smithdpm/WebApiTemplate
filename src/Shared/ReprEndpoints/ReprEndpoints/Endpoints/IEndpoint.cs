using Microsoft.AspNetCore.Routing;

namespace ReprEndpoints.Endpoints;
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);

}
