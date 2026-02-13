using Cqrs.DomainTestFixture;
using Cqrs.EntityFrameworkCore.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Cqrs.ApplicationTestFixture.Database;

public class ApplicationDbContext: DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Item> Items { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.ApplyOutboxConfiguration();
    }
}
