namespace Application.Behaviours.RepositoryCaching;
public interface ICacheInvalidationPolicy 
{
    Type EntityType { get; }
    IEnumerable<string> GetKeysToInvalidate(ChangedEntity changedEntity);
}
