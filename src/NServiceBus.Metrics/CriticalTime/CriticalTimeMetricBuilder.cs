using System;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;

class CriticalTimeMetricBuilder : IMetricBuilder
{
    public void WireUp(FeatureConfigurationContext featureConfigurationContext, MetricsContext metricsContext, Unit messagesUnit)
    {
        var criticalTimeTimer = metricsContext.Timer("Critical Time", messagesUnit);

        featureConfigurationContext.Pipeline.OnReceivePipelineCompleted(e =>
        {
            DateTime timeSent;
            if (e.TryGetTimeSent(out timeSent))
            {
                var endToEndTime = e.CompletedAt - timeSent;

                criticalTimeTimer.Record((long)endToEndTime.TotalMilliseconds, TimeUnit.Milliseconds);
            }

            return Task.FromResult(0);
        });
    }
}
