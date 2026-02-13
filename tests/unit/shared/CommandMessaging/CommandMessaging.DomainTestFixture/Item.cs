using SharedKernel.Events;

namespace Cqrs.DomainTestFixture;

public class Item: HasDomainEvents
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
    public string Name { get; private set; } = null!;

    public Item(string name)
    {
        Name = name;
    }
    public void UpdateName(string newName)
    {
        var domainEvent = new ItemNameUpdatedDomainEvent(Id, Name, newName);
        Name = newName;
        AddDomainEvent(domainEvent);
    }

}
