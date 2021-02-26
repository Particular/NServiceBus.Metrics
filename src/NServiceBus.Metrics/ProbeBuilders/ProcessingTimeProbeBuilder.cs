using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

[ProbeProperties(ProcessingTime, "The time it took to successfully process a message.")]
class ProcessingTimeProbeBuilder : DurationProbeBuilder
{
    public ProcessingTimeProbeBuilder(FeatureConfigurationContext context)
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

            e.TryGetMessageType(out var messageTypeProcessed);

            var processingTime = e.CompletedAt - e.StartedAt;

            var @event = new DurationEvent(processingTime, messageTypeProcessed);

            probe.Record(ref @event);

            return TaskExtensions.Completed;
        });
    }

    FeatureConfigurationContext context;

    public const string ProcessingTime = "Processing Time";
}