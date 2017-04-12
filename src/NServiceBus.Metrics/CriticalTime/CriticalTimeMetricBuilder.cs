using System;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;

class CriticalTimeMetricBuilder : IMetricBuilder
{
    Timer criticalTimeTimer;

    public void Define(MetricsContext metricsContext)
    {
        criticalTimeTimer = metricsContext.Timer("Critical Time", Unit.Custom("Messages"));
    }

    public void WireUp(FeatureConfigurationContext featureConfigurationContext)
    {
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
