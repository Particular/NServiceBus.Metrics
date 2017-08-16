using System;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

[ProbeProperties(Probes.CriticalTime, CriticalTime)]
class CriticalTimeProbeBuilder : DurationProbeBuilder
{
    const string CriticalTime = "Critical Time";

    public CriticalTimeProbeBuilder(FeatureConfigurationContext context)
    {
        this.context = context;
    }

    protected override void WireUp(DurationProbe probe)
    {
        context.Pipeline.OnReceivePipelineCompleted(e =>
        {
            DateTime timeSent;
            if (e.TryGetTimeSent(out timeSent))
            {
                var endToEndTime = e.CompletedAt - timeSent;

                probe.Record(endToEndTime);
            }

            return TaskExtensions.Completed;
        });
    }

    readonly FeatureConfigurationContext context;
}