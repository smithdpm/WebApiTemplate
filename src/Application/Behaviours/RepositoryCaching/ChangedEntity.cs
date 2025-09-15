
using Ardalis.Specification;
using Domain;
using SharedKernel.Database;

namespace Application.Behaviours.RepositoryCaching;
public record ChangedEntity<T, TId>
(TId Id, Type EntityType, T? Before, T? After)
    where TId : struct, IEquatable<TId>
    where T : Entity<TId>, IAggregateRoot;


