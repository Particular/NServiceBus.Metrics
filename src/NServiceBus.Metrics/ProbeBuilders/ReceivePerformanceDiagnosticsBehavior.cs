using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Pipeline;

class ReceivePerformanceDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
{
    public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        context.MessageHeaders.TryGetMessageType(out var messageType);

        var @event = new SignalEvent(messageType);

        MessagePulledFromQueue?.Record(ref @event);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception)
        {
            ProcessingFailure?.Record(ref @event);
            throw;
        }

        ProcessingSuccess?.Record(ref @event);
    }

    public SignalProbe MessagePulledFromQueue;
    public SignalProbe ProcessingFailure;
    public SignalProbe ProcessingSuccess;
}