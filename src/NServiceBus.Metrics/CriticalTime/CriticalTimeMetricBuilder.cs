using System;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

class CriticalTimeMetricBuilder : MetricBuilder
{
    readonly ResetMetricTimer resetMetricTimer;

    public CriticalTimeMetricBuilder(ResetMetricTimer resetMetricTimer)
    {
        this.resetMetricTimer = resetMetricTimer;
    }

    public override void WireUp(FeatureConfigurationContext featureConfigurationContext)
    {
        resetMetricTimer.NoMessageSentForAWhile += (sender, message) =>
        {
            criticalTimeTimer.Record(0, TimeUnit.Milliseconds);
        };

        featureConfigurationContext.Pipeline.OnReceivePipelineCompleted(e =>
        {
            DateTime timeSent;
            if (e.TryGetTimeSent(out timeSent))
            {
                var endToEndTime = e.CompletedAt - timeSent;

                criticalTimeTimer.Record((long) endToEndTime.TotalMilliseconds, TimeUnit.Milliseconds);
            }

            return Task.FromResult(0);
        });
    }

    [Timer("Critical Time", "Messages", "The time it took from sending to processing the message.")]
    Timer criticalTimeTimer = default(Timer);
}