using Domain.Cars;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Database;
using System.Reflection;

namespace Infrastructure.Database;

public class ApplicationContext: DbContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options) : base(options) { }

    public DbSet<Car> Cars { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.ApplyOutboxConfiguration();
    }
}