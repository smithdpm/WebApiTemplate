using Cqrs.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.EntityFrameworkCore.Configuration;
public static class OutboxModelBuilderExtensions
{
    public static void ApplyOutboxConfiguration(this ModelBuilder modelBuilder, string schema = "outbox")
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages", schema);
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.HasIndex(x => new
            {
                x.ProcessedAtUtc,
                x.LockedUntilUtc
            })
            .HasFilter("[ProcessedAtUtc] IS NULL")
            .HasDatabaseName("IX_Outbox_Unprocessed");
        });
    }
}
