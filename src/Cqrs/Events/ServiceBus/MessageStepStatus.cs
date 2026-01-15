

namespace Cqrs.Events.ServiceBus;
internal enum MessageStepStatus
{
    Success,
    DeadLetter,
    Skip
}
