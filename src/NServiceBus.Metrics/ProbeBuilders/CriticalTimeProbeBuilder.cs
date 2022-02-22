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
        context.Pipeline.OnReceivePipelineCompleted((e, _) =>
        {
            if (e.TryGetDeliverAt(out DateTimeOffset startTime) || e.TryGetTimeSent(out startTime))
            {
                var endToEndTime = e.CompletedAt - startTime;
                e.TryGetMessageType(out var messageType);

                var @event = new DurationEvent(endToEndTime, messageType);
                probe.Record(ref @event);
            }

            return TaskExtensions.Completed;
        });
    }

    readonly FeatureConfigurationContext context;
}
