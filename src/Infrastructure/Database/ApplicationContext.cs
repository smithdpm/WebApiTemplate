using Domain.Cars;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Events;
using System.Reflection;

namespace Infrastructure.Database;

public class ApplicationContext: DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

    public DbSet<Car> Cars { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.ProcessedAtUtc, x.LockedUntilUtc })
                   .HasFilter("[ProcessedAtUtc] IS NULL")
                   .HasDatabaseName("IX_Outbox_Unprocessed");
        });
    }
}