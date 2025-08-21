using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Infrastructure.Authorization;

internal class PermissionAutherizationHandler(IServiceScopeFactory serviceScopeFactory) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        // TODO: Once Entra Authentication is configured, replace the hardcoded userId with the authenticated user's ID.
        // and uncomment the code to fail unauthenticated users.

        // Remove this
        var userId = new Guid();

        // Uncomment this
        //if (context.User is { Identity.IsAuthenticated : false })
        //{
        //    context.Fail();
        //    return;
        //}

        //var userId = Guid.Parse(context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");

        using IServiceScope scope = serviceScopeFactory.CreateScope();

        var permissionProvider = scope.ServiceProvider.GetRequiredService<PermissionProvider>();

        var permissions = await permissionProvider.GetByUserIdAsync(userId);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
