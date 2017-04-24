using System;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

class CriticalTimeMetricBuilder : MetricBuilder
{
    public override void WireUp(FeatureConfigurationContext featureConfigurationContext)
    {
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

    [Timer("Critical Time", "Messages", "Age of the oldest message in the queue.")]
    Timer criticalTimeTimer = default(Timer);
}