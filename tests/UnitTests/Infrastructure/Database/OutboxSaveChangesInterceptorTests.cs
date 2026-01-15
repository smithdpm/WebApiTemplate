using Cqrs.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SharedKernel.Events;
using Shouldly;
using System.Text.Json;

namespace UnitTests.Infrastructure.Database;

public class OutboxSaveChangesInterceptorTests
{
    private readonly OutboxSaveChangesInterceptor _interceptor;

    public OutboxSaveChangesInterceptorTests()
    {
        _interceptor = new OutboxSaveChangesInterceptor();
    }

    [Fact]
    public async Task SavingChangesAsync_ShouldCaptureAndClearDomainEvents_WhenEntitiesHaveEvents()
    {
        // Arrange  
        var entityWithEvents = new TestEntityWithEvents("Test Entity");
        var testEvent = new TestDomainEvent { EntityId = entityWithEvents.Id, Name = "Test Event" };
        entityWithEvents.AddDomainEvent(testEvent);

        using var testDbContext = CreateTestDbContext(_interceptor);
        testDbContext.Add(entityWithEvents);

        // Act
        await testDbContext.SaveChangesAsync(CancellationToken.None);
        
        // Assert
        entityWithEvents.DomainEvents.ShouldBeEmpty();
        testDbContext.OutboxMessages.Count().ShouldBe(1);
        var capturedMessage = testDbContext.OutboxMessages.First();
        capturedMessage.EventType.ShouldBe(nameof(TestDomainEvent));
        var deserializedEvent = JsonSerializer.Deserialize<TestDomainEvent>(capturedMessage.Payload);
        deserializedEvent.ShouldBeEquivalentTo(testEvent);
    }

    [Fact]
    public async Task SavingChangesAsync_ShouldNotAddOutboxMessages_WhenNoEventsPresent()
    {
        // Arrange
        var entityWithoutEvents = new TestEntityWithEvents("Entity Without Events");

        using var testDbContext = CreateTestDbContext(_interceptor);
        testDbContext.Add(entityWithoutEvents);

        // Act
        await testDbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        testDbContext.OutboxMessages.Count().ShouldBe(0);
    }

    [Fact]
    public async Task GetDomainEvents_ShouldReturnAllEvents_WhenMultipleEntitiesHaveEvents()
    {
        // Arrange
        var entity1 = new TestEntityWithEvents("Entity 1");
        var entity2 = new TestEntityWithEvents("Entity 2");

        entity1.AddDomainEvent(new TestDomainEvent { EntityId = entity1.Id, Name = "Event1" });
        entity2.AddDomainEvent(new TestDomainEvent { EntityId = entity2.Id, Name = "Event2" });
        entity2.AddDomainEvent(new TestDomainEvent { EntityId = entity2.Id, Name = "Event3" });

        using var testDbContext = CreateTestDbContext(_interceptor);
        testDbContext.AddRange(entity1, entity2);

        // Act
        await testDbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        entity1.DomainEvents.ShouldBeEmpty();
        entity2.DomainEvents.ShouldBeEmpty();
        testDbContext.OutboxMessages.Count().ShouldBe(3);
        
        var capturedMessages = testDbContext.OutboxMessages.ToList();
        capturedMessages.All(m => m.EventType == nameof(TestDomainEvent)).ShouldBeTrue();
    }

    [Fact]
    public async Task DomainEventsToOutboxMessages_ShouldPreserveEventOrder_WhenMultipleEventsProvided()
    {
        // Arrange
        var entity = new TestEntityWithEvents("Test Entity");
        var events = new List<TestDomainEvent>
        {
            new() { EntityId = entity.Id, Name = "Event1" },
            new() { EntityId = entity.Id, Name = "Event2" },
            new() { EntityId = entity.Id, Name = "Event3" }
        };

        foreach (var evt in events)
            entity.AddDomainEvent(evt);

        using var testDbContext = CreateTestDbContext(_interceptor);
        testDbContext.Add(entity);

        // Act
        await testDbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        entity.DomainEvents.ShouldBeEmpty();
        testDbContext.OutboxMessages.Count().ShouldBe(3);

        var capturedMessages = testDbContext.OutboxMessages.OrderBy(m => m.Id).ToList();
        for (int i = 0; i < events.Count; i++)
        {
            var payload = JsonSerializer.Deserialize<TestDomainEvent>(capturedMessages[i].Payload);
            payload.Name.ShouldBe(events[i].Name);
        }
    }

    [Fact]
    public async Task DomainEventsToOutboxMessages_ShouldSerializeCorrectly_WhenEventsContainComplexData()
    {
        // Arrange
        var complexEvent = new ComplexDomainEvent
        {
            Id = Guid.NewGuid(),
            Name = "Complex Event",
            NestedData = new NestedData
            {
                Value = 42,
                Description = "Test Description",
                Tags = new List<string> { "tag1", "tag2", "tag3" }
            },
            Items = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 123,
                ["key3"] = true
            }
        };

        var entity = new TestEntityWithEvents("Complex Event Entity");
        entity.AddDomainEvent(complexEvent);

        using var testDbContext = CreateTestDbContext(_interceptor);
        testDbContext.Add(entity);

        // Act
        await testDbContext.SaveChangesAsync(CancellationToken.None);

        // Assert
        entity.DomainEvents.ShouldBeEmpty();
        testDbContext.OutboxMessages.Count().ShouldBe(1);

        var outboxMessage = testDbContext.OutboxMessages.First();
        outboxMessage.EventType.ShouldBe(nameof(ComplexDomainEvent));

        var deserializedEvent = JsonSerializer.Deserialize<ComplexDomainEvent>(outboxMessage.Payload)!;
        deserializedEvent.Id.ShouldBe(complexEvent.Id);
        deserializedEvent.Name.ShouldBe(complexEvent.Name);
        deserializedEvent.NestedData.Value.ShouldBe(complexEvent.NestedData.Value);
        deserializedEvent.NestedData.Description.ShouldBe(complexEvent.NestedData.Description);
        deserializedEvent.NestedData.Tags.Count.ShouldBe(3);
    }

    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntityWithEvents> TestEntitiesWithEvents { get; set; }
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntityWithEvents>().HasKey(e => e.Id);
        }
    }
    private static TestDbContext CreateTestDbContext(IInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;
        return new TestDbContext(options);
    }

    internal class TestEntityWithEvents : HasDomainEvents
    {
        public TestEntityWithEvents() { }
        public TestEntityWithEvents(string name) => Name = name;

        public Guid Id { get; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
    }

    internal record TestDomainEvent : DomainEventBase
    {
        public Guid EntityId { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    internal record ComplexDomainEvent : DomainEventBase
    {
        public new Guid Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public NestedData NestedData { get; init; } = new();
        public Dictionary<string, object> Items { get; init; } = new();
    }

    internal class NestedData
    {
        public int Value { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }
}