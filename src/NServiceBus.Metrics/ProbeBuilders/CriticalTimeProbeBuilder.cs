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
        context.OnReceiveCompleted((e, _) =>
        {
            if (!e.WasAcknowledged || e.OnMessageFailed)
            {
                return TaskExtensions.Completed;
            }

            if (e.TryGetTimeSent(out var timeSent))
            {
                var endToEndTime = e.CompletedAt - timeSent;
                e.TryGetMessageType(out var messageType);

                var @event = new DurationEvent(endToEndTime, messageType);
                probe.Record(ref @event);
            }

            return TaskExtensions.Completed;
        });
    }

    readonly FeatureConfigurationContext context;
}