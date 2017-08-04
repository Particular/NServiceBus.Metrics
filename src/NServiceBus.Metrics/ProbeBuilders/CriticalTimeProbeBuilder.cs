using System;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

[ProbeProperties(CriticalTimeId, CriticalTime, "The time it took from sending to processing the message.")]
class CriticalTimeProbeBuilder : DurationProbeBuilder
{
    public const string CriticalTimeId = "nservicebus_criticaltime_seconds";
    public const string CriticalTime = "Critical Time";

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