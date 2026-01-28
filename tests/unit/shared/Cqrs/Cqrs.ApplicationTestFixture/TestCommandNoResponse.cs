using Cqrs.Messaging;

namespace Cqrs.ApplicationTestFixture;

public class TestCommandNoResponse : ICommand
{
    public Guid Id { get; set; }
}