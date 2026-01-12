using Microsoft.EntityFrameworkCore;
using SharedKernel.Database;

namespace ComponentTests.TestCollections.Fixtures;

public class TestOutboxContext : DbContext
{
    public TestOutboxContext(DbContextOptions<TestOutboxContext> options) : base(options) { }

    public DbSet<SharedKernel.Events.OutboxMessage> OutboxMessages { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyOutboxConfiguration();
    }
}