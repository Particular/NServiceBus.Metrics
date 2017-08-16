using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

[ProbeProperties(Probes.ProcessingTime, ProcessingTime)]
class ProcessingTimeProbeBuilder : DurationProbeBuilder
{
    const string ProcessingTime = "Processing Time";

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

            return TaskExtensions.Completed;
        });
    }

    readonly FeatureConfigurationContext context;
}