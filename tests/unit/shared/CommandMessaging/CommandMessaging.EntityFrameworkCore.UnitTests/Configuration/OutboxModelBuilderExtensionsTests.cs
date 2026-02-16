using Cqrs.EntityFrameworkCore.Configuration;
using Cqrs.Outbox;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Cqrs.EntityFrameworkCore.UnitTests.Configuration;

public class OutboxModelBuilderExtensionsTests
{
    [Fact]
    public void ConfigureOutbox_ShouldSetupEntity()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;
        
        using var context = new TestDbContext(options);

        // Act
        var model = context.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(OutboxMessage));
        entityType.ShouldNotBeNull();
        
        var tableName = entityType.GetTableName();
        tableName.ShouldBe("OutboxMessages");
        
        var schema = entityType.GetSchema();
        schema.ShouldBe("outbox");
        
        var key = entityType.FindPrimaryKey();
        key.ShouldNotBeNull();
        key.Properties.Count.ShouldBe(1);
        key.Properties[0].Name.ShouldBe("Id");
    }

    [Fact]
    public void ConfigureOutbox_ShouldSetIndexes()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDbIndexes")
            .Options;
        
        using var context = new TestDbContext(options);

        // Act
        var model = context.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(OutboxMessage));
        entityType.ShouldNotBeNull();
        
        var indexes = entityType.GetIndexes();
        indexes.ShouldNotBeEmpty();
        
        var unprocessedIndex = indexes.FirstOrDefault(i => i.GetDatabaseName() == "IX_Outbox_Unprocessed");
        unprocessedIndex.ShouldNotBeNull();
        
        var indexProperties = unprocessedIndex.Properties.Select(p => p.Name).ToList();
        indexProperties.ShouldContain("ProcessedAtUtc");
        indexProperties.ShouldContain("LockedUntilUtc");
    }

    [Fact]
    public void ConfigureOutbox_ShouldUseDefinedSchema_WhenCustomSchemaIsProvided()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TestDbContextWithCustomSchema>()
            .UseInMemoryDatabase(databaseName: "TestDbCustomSchema")
            .Options;
        
        using var context = new TestDbContextWithCustomSchema(options);
        // Act
        var model = context.Model;

        // Assert
        var entityType = model.FindEntityType(typeof(OutboxMessage));
        entityType.ShouldNotBeNull();
        
        var schema = entityType.GetSchema();
        schema.ShouldBe("custom_schema");
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyOutboxConfiguration();
        }
    }

    private class TestDbContextWithCustomSchema : DbContext
    {
        public TestDbContextWithCustomSchema(DbContextOptions<TestDbContextWithCustomSchema> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyOutboxConfiguration(schema: "custom_schema");
        }
    }
}