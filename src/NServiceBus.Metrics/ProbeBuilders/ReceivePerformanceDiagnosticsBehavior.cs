using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Pipeline;

class ReceivePerformanceDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
{
    public Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        context.MessageHeaders.TryGetMessageType(out var messageType);

        var @event = new SignalEvent(messageType);

        MessagePulledFromQueue?.Signal(ref @event);

        return next(context);
    }

    public SignalProbe MessagePulledFromQueue;
}