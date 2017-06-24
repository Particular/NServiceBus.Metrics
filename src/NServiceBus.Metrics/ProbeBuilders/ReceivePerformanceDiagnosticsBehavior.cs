using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Pipeline;

class ReceivePerformanceDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
{
    public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        MessagePulledFromQueue?.Signal();

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception)
        {
            ProcessingFailure?.Signal();
            throw;
        }

        ProcessingSuccess?.Signal();
    }

    public SignalProbe MessagePulledFromQueue;
    public SignalProbe ProcessingFailure;
    public SignalProbe ProcessingSuccess;
}