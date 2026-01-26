using SharedKernel.Abstractions;
using SharedKernel.Events;

namespace Domain;

public abstract class Entity<TId> : HasDomainEvents, IEntity<TId>, IHasId where TId : struct, IEquatable<TId>
{
    public TId Id { get; protected init; }

    protected Entity(TId id)
    {
        Id = id;
    }

    public string GetId()
        => Id.ToString()!; 

}
