using Microsoft.EntityFrameworkCore;
using Cqrs.Outbox;
using Cqrs.EntityFrameworkCore.Configuration;

namespace Cqrs.IntegrationTests.TestCollections.Fixtures;

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