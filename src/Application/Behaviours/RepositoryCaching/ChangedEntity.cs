
using SharedKernel.Abstractions;

namespace Application.Behaviours.RepositoryCaching;
public record ChangedEntity<T>
    (string Id,
    T? Before,
    T? After)
    where T : IHasId;