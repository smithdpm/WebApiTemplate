﻿using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Authorization;

internal class PermissionRequirement: IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
