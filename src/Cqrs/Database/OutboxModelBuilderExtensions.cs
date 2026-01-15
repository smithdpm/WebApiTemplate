
using Microsoft.EntityFrameworkCore;
using Cqrs.Events;

namespace Cqrs.Database;
public static class OutboxModelBuilderExtensions
{
    public static void ApplyOutboxConfiguration(this ModelBuilder modelBuilder, string schema = "outbox")
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages", schema);
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.HasIndex(x => new {
                x.ProcessedAtUtc,
                x.LockedUntilUtc
            })
                   .HasFilter("[ProcessedAtUtc] IS NULL")
                   .HasDatabaseName("IX_Outbox_Unprocessed");
        });
    }
}
