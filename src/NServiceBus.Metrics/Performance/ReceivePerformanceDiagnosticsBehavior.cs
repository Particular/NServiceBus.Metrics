using System;
using System.Threading.Tasks;
using NServiceBus.Metrics;
using NServiceBus.Pipeline;

class ReceivePerformanceDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
{
    public ReceivePerformanceDiagnosticsBehavior(Probe messagesPulledFromQueueMeter, Probe failureRateMeter, Probe successRateMeter)
    {
        this.messagesPulledFromQueueMeter = messagesPulledFromQueueMeter;
        this.failureRateMeter = failureRateMeter;
        this.successRateMeter = successRateMeter;
    }

    public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        messagesPulledFromQueueMeter.Record(1);

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception)
        {
            failureRateMeter.Record(1);
            throw;
        }

        successRateMeter.Record(1);
    }

    Probe messagesPulledFromQueueMeter;
    Probe failureRateMeter;
    Probe successRateMeter;
}