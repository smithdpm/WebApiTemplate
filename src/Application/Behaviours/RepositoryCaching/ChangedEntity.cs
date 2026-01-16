namespace Application.Behaviours.RepositoryCaching;
public record ChangedEntity
    (string Id,
    Type EntityType,
    object? Before,
    object? After);