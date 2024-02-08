using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;

[ProbeProperties(CriticalTime, "The time it took from sending to processing the message.")]
class CriticalTimeProbeBuilder(FeatureConfigurationContext context) : DurationProbeBuilder
{
    protected override void WireUp(DurationProbe probe) =>
        context.Pipeline.OnReceivePipelineCompleted((e, _) =>
        {
            if (e.TryGetDeliverAt(out DateTimeOffset startTime) || e.TryGetTimeSent(out startTime))
            {
                var endToEndTime = e.CompletedAt - startTime;
                e.TryGetMessageType(out var messageType);

                var @event = new DurationEvent(endToEndTime, messageType);
                probe.Record(ref @event);
            }

            return Task.CompletedTask;
        });

    public const string CriticalTime = "Critical Time";
}
