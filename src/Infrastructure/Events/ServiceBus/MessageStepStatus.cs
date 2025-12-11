

namespace Infrastructure.Events.ServiceBus;
internal enum MessageStepStatus
{
    Success,
    DeadLetter,
    Skip
}
