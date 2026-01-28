using Cqrs.Messaging;

namespace Cqrs.ApplicationTestFixture;

public class TestQuery : IQuery<string>
{
    public Guid Id { get; set; }
}