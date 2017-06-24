using NServiceBus;
using NServiceBus.Features;

[ProbeProperties(ProbeType.Duration, "Processing Time", "The time it took to successfully process a message.")]
class ProcessingTimeProbeBuilder : DurationProbeBuilder
{
    public ProcessingTimeProbeBuilder(FeatureConfigurationContext context)
    {
        this.context = context;
    }

    protected override void WireUp(DurationProbe probe)
    {
        context.Pipeline.OnReceivePipelineCompleted(e =>
        {
            string messageTypeProcessed;
            e.TryGetMessageType(out messageTypeProcessed);

            var processingTime = e.CompletedAt - e.StartedAt;

            probe.Record(processingTime);

            return CompletedTask;
        });
    }

    readonly FeatureConfigurationContext context;
}