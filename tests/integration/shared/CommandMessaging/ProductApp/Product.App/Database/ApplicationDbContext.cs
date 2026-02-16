
using Cqrs.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using Product.App.Model;

namespace Product.App.Database;

public class ApplicationDbContext: DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ProductItem> ProductItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.ApplyOutboxConfiguration();
    }

}
