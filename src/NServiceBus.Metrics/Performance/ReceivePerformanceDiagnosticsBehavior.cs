using System;
using System.Threading.Tasks;
using Metrics;
using NServiceBus.Pipeline;

class ReceivePerformanceDiagnosticsBehavior : IBehavior<IIncomingPhysicalMessageContext, IIncomingPhysicalMessageContext>
{
    public ReceivePerformanceDiagnosticsBehavior(Meter messagesPulledFromQueueMeter, Meter failureRateMeter, Meter successRateMeter)
    {
        this.messagesPulledFromQueueMeter = messagesPulledFromQueueMeter;
        this.failureRateMeter = failureRateMeter;
        this.successRateMeter = successRateMeter;
    }

    public async Task Invoke(IIncomingPhysicalMessageContext context, Func<IIncomingPhysicalMessageContext, Task> next)
    {
        messagesPulledFromQueueMeter.Mark();

        try
        {
            await next(context).ConfigureAwait(false);
        }
        catch (Exception)
        {
            failureRateMeter.Mark();
            throw;
        }

        successRateMeter.Mark();
    }

    Meter messagesPulledFromQueueMeter;
    Meter failureRateMeter;
    Meter successRateMeter;
}