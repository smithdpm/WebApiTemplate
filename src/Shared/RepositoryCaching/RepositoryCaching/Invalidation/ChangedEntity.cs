namespace RepositoryCaching.Invalidation;
public record ChangedEntity
    (string Id,
    Type EntityType,
    object? Before,
    object? After);