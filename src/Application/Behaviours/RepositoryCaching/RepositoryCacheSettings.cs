
namespace Application.Behaviours.RepositoryCaching;

public class RepositoryCacheSettings
{
    public bool Enabled { get; set; } = false;
    public int DefaultExpirationInMinutes { get; set; } = 5;

    public Dictionary<string, EntityCacheSettings> PerEntitySettings { get; set; } = new();

}

