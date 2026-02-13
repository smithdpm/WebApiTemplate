using Domain.Abstractions;

namespace Domain.UnitTests.TestHelpers;

internal class IdGenerator : IIdGenerator<Guid>
{
    public Guid NewId()
    {
        return Guid.NewGuid();
    }
}