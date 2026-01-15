using Cqrs.Events;
using Microsoft.EntityFrameworkCore;
using Cqrs.Database;

namespace ComponentTests.TestCollections.Fixtures;

public class TestOutboxContext : DbContext
{
    public TestOutboxContext(DbContextOptions<TestOutboxContext> options) : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyOutboxConfiguration();
    }
}