using System;
using NServiceBus;
using NServiceBus.Features;

[ProbeProperties(ProbeType.Duration, "Critical Time", "Time between message sent till end of processing.")]
class CriticalTimeProbeBuilder : DurationProbeBuilder
{
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

            return CompletedTask;
        });
    }

    readonly FeatureConfigurationContext context;
}