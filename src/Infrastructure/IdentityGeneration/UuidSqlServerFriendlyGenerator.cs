using Domain.Abstractions;

using UUIDNext;

namespace Infrastructure.IdentityGeneration;
internal class UuidSqlServerFriendlyGenerator : IIdGenerator<Guid>
{
    public Guid NewId()
    {
        return Uuid.NewDatabaseFriendly(UUIDNext.Database.SqlServer);
    }
}
