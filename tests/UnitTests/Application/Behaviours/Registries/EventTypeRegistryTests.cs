using Application.Behaviours.Registries;
using SharedKernel.Events.DomainEvents;
using SharedKernel.Events.IntegrationEvents;
using Shouldly;

namespace UnitTests.Application.Behaviours.Registries;

public class EventTypeRegistryTests
{
    private readonly EventTypeRegistry _registry;

    public EventTypeRegistryTests()
    {
        _registry = new EventTypeRegistry();
    }

    [Fact]
    public void GetTypeByName_ShouldReturnType_WhenTypeHasBeenRegistered()
    {
        // Arrange
        var types = new Type[] { typeof(TestDomainEvent) };
        _registry.RegisterDomainEventsFromAssemblyTypes(types);

        // Act
        var result = _registry.GetTypeByName(nameof(TestDomainEvent));

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe(typeof(TestDomainEvent));
    }

    [Fact]
    public void GetTypeByName_ShouldReturnNull_WhenTypeNotRegistered()
    {
        // Arrange
        // Registry is empty

        // Act
        var result = _registry.GetTypeByName("NonExistentEvent");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void RegisterDomainEventsFromAssemblyTypes_ShouldOnlyRegisterDomainEventTypes()
    {
        // Arrange
        var types = new Type[]
        {
            typeof(TestDomainEvent),
            typeof(TestIntegrationEvent),
            typeof(NonEventClass),
            typeof(string)
        };

        // Act
        _registry.RegisterDomainEventsFromAssemblyTypes(types);

        // Assert
        _registry.GetTypeByName(nameof(TestDomainEvent)).ShouldNotBeNull();
        _registry.GetTypeByName(nameof(TestIntegrationEvent)).ShouldBeNull();
        _registry.GetTypeByName(nameof(NonEventClass)).ShouldBeNull();
        _registry.GetTypeByName(nameof(String)).ShouldBeNull();
    }

    [Fact]
    public void RegisterDomainEventsFromAssemblyTypes_ShouldOnlyRegisterConcreteImplementations()
    {
        // Arrange
        var types = new Type[]
        {
            typeof(TestDomainEvent),
            typeof(AbstractDomainEvent),
            typeof(IDomainEvent)
        };

        // Act
        _registry.RegisterDomainEventsFromAssemblyTypes(types);

        // Assert
        _registry.GetTypeByName(nameof(TestDomainEvent)).ShouldNotBeNull();
        _registry.GetTypeByName(nameof(AbstractDomainEvent)).ShouldBeNull();
        _registry.GetTypeByName(nameof(IDomainEvent)).ShouldBeNull();
    }

    [Fact]
    public void RegisterDomainEventsFromAssemblyTypes_ShouldRegisterNoTypes_WhenNoValidTypesPassed()
    {
        // Arrange
        var types = new Type[]
        {
            typeof(NonEventClass),
            typeof(string),
            typeof(int)
        };

        // Act
        _registry.RegisterDomainEventsFromAssemblyTypes(types);

        // Assert
        _registry.GetTypeByName(nameof(NonEventClass)).ShouldBeNull();
        _registry.GetTypeByName(nameof(String)).ShouldBeNull();
    }

    [Fact]
    public void RegisterIntegrationEventsFromAssemblyTypes_ShouldOnlyRegisterIntegrationEventTypes()
    {
        // Arrange
        var types = new Type[]
        {
            typeof(TestIntegrationEvent),
            typeof(TestDomainEvent),
            typeof(NonEventClass),
            typeof(string)
        };

        // Act
        _registry.RegisterIntegrationEventsFromAssemblyTypes(types);

        // Assert
        _registry.GetTypeByName(nameof(TestIntegrationEvent)).ShouldNotBeNull();
        _registry.GetTypeByName(nameof(TestDomainEvent)).ShouldBeNull();
        _registry.GetTypeByName(nameof(NonEventClass)).ShouldBeNull();
        _registry.GetTypeByName(nameof(String)).ShouldBeNull();
    }

    [Fact]
    public void RegisterIntegrationEventsFromAssemblyTypes_ShouldOnlyRegisterConcreteImplementations()
    {
        // Arrange
        var types = new Type[]
        {
            typeof(TestIntegrationEvent),
            typeof(AbstractIntegrationEvent),
            typeof(IIntegrationEvent)
        };

        // Act
        _registry.RegisterIntegrationEventsFromAssemblyTypes(types);

        // Assert
        _registry.GetTypeByName(nameof(TestIntegrationEvent)).ShouldNotBeNull();
        _registry.GetTypeByName(nameof(AbstractIntegrationEvent)).ShouldBeNull();
        _registry.GetTypeByName(nameof(IIntegrationEvent)).ShouldBeNull();
    }

    [Fact]
    public void RegisterIntegrationEventsFromAssemblyTypes_ShouldRegisterNoTypes_WhenNoValidTypesPassed()
    {
        // Arrange
        var types = new Type[]
        {
            typeof(NonEventClass),
            typeof(string),
            typeof(int)
        };

        // Act
        _registry.RegisterIntegrationEventsFromAssemblyTypes(types);

        // Assert
        _registry.GetTypeByName(nameof(NonEventClass)).ShouldBeNull();
        _registry.GetTypeByName(nameof(String)).ShouldBeNull();
    }

    [Fact]
    public void Registry_ShouldSupportBothEventTypes_WhenBothRegistered()
    {
        // Arrange
        var domainTypes = new Type[] { typeof(TestDomainEvent), typeof(AnotherDomainEvent) };
        var integrationTypes = new Type[] { typeof(TestIntegrationEvent), typeof(AnotherIntegrationEvent) };

        // Act
        _registry.RegisterDomainEventsFromAssemblyTypes(domainTypes);
        _registry.RegisterIntegrationEventsFromAssemblyTypes(integrationTypes);

        // Assert
        _registry.GetTypeByName(nameof(TestDomainEvent)).ShouldBe(typeof(TestDomainEvent));
        _registry.GetTypeByName(nameof(AnotherDomainEvent)).ShouldBe(typeof(AnotherDomainEvent));
        _registry.GetTypeByName(nameof(TestIntegrationEvent)).ShouldBe(typeof(TestIntegrationEvent));
        _registry.GetTypeByName(nameof(AnotherIntegrationEvent)).ShouldBe(typeof(AnotherIntegrationEvent));
    }

    private record TestDomainEvent : DomainEventBase, IDomainEvent
    {
        public new Guid Id { get; init; }
    }

    private record AnotherDomainEvent : DomainEventBase, IDomainEvent
    {
        public string Data { get; init; } = string.Empty;
    }

    private abstract record AbstractDomainEvent : DomainEventBase, IDomainEvent
    {
        public abstract void Process();
    }

    private record TestIntegrationEvent : IntegrationEventBase, IIntegrationEvent
    {
        public Guid EventId { get; init; }
    }

    private record AnotherIntegrationEvent : IntegrationEventBase, IIntegrationEvent
    {
        public string Message { get; init; } = string.Empty;
    }

    private abstract record AbstractIntegrationEvent : IntegrationEventBase, IIntegrationEvent
    {
        public abstract void Handle();
    }

    private class NonEventClass
    {
        public string Name { get; set; } = string.Empty;
    }
}