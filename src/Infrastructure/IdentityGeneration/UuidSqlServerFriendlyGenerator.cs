using Domain.Abstractions;

using UUIDNext;

namespace Infrastructure.IdentityGeneration;
public class UuidSqlServerFriendlyGenerator : IIdGenerator<Guid>
{
    public Guid NewId()
    {
        return Uuid.NewDatabaseFriendly(UUIDNext.Database.SqlServer);
    }
}
