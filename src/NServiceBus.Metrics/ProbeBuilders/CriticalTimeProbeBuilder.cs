using System;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

[ProbeProperties(CriticalTime, "The time it took from sending to processing the message.")]
class CriticalTimeProbeBuilder : DurationProbeBuilder
{
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
                string messageType;
                e.TryGetMessageType(out messageType);

                var @event = new DurationEvent(endToEndTime, messageType);
                probe.Record(ref @event);
            }

            return TaskExtensions.Completed;
        });
    }

    readonly FeatureConfigurationContext context;
}