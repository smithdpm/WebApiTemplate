namespace Infrastructure.Authorization;

internal class PermissionProvider
{
    // TODO: Create a real PermissionProvider here
    // Can fetch user permissions from a database or a centralised service
    public Task<HashSet<string>> GetByUserIdAsync(Guid userId)
    {
        var permissions = new HashSet<string>(Permissions);
        return Task.FromResult(permissions);
    }

    public readonly string[] Permissions = new[]
    {
        "Cars.Create",
        "Cars.Update",
        "Cars.Delete",
        "Cars.View",
        "Cars.List",
        "Cars.Sell"
    };
}
