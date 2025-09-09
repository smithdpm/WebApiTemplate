﻿

namespace Application.Behaviours.RepositoryCaching;

public class EntityCacheSettings
{
    public bool Enabled { get; set; } = true;
    public int? ExpirationInMinutes { get; set; }
}
