namespace RepositoryCaching.Invalidation.Policies;
public interface ICacheInvalidationPolicy 
{
    Type EntityType { get; }
    IEnumerable<string> GetKeysToInvalidate(ChangedEntity changedEntity);
}
