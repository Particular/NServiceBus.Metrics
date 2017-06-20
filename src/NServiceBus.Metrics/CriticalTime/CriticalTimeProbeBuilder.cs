using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

class CriticalTimeProbeBuilder : ProbeBuilder
{
    public override Probe[] WireUp(FeatureConfigurationContext featureConfigurationContext)
    {
        var criticalTime = new Probe("Critical Time", MeasurementValueType.Time, "Time it took for a single message from sending until end on processing.");

        featureConfigurationContext.Pipeline.OnReceivePipelineCompleted(e =>
        {
            DateTime timeSent;
            if (e.TryGetTimeSent(out timeSent))
            {
                var endToEndTime = e.CompletedAt - timeSent;

                criticalTime.Record((long) endToEndTime.TotalMilliseconds);
            }

            return Task.FromResult(0);
        });

        return new[]{criticalTime};
    }
}