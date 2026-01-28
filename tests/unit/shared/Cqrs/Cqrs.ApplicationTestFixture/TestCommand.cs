using Cqrs.Messaging;

namespace Cqrs.ApplicationTestFixture;

public class TestCommand : ICommand<string>
{
    public Guid Id { get; set; }
}