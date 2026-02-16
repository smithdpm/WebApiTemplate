
using Cqrs.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;
using Shop.Domain.Aggregates.Purchases;
using Shop.Domain.Aggregates.Stock;
using System.Reflection;

namespace Shop.Application.Database;

public class ApplicationDbContext: DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Purchase> Purchases { get; set; }
    public DbSet<ProductStock> ProductStocks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.ApplyOutboxConfiguration();

        modelBuilder.Entity<Purchase>().OwnsMany(
            p=> p.SoldProducts, sp =>
            {
                sp.WithOwner().HasForeignKey("PurchaseId");
                sp.Property<int>("Id");
                sp.HasKey("Id");
            });
    }

}
