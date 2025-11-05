using SharedKernel.Events.DomainEvents;

namespace Domain;

public abstract class Entity<TId> : HasDomainEvents, IEntity<TId> where TId : struct, IEquatable<TId>
{
    public TId Id { get; protected set; }

    protected Entity(TId id)
    {
        Id = id;
    }
}
