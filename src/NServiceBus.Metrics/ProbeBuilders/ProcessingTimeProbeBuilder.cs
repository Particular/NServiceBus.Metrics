using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;

[ProbeProperties(ProcessingTime, "The time it took to successfully process a message.")]
class ProcessingTimeProbeBuilder(FeatureConfigurationContext context) : DurationProbeBuilder
{
    protected override void WireUp(DurationProbe probe) =>
        context.Pipeline.OnReceivePipelineCompleted((e, _) =>
        {
            e.TryGetMessageType(out var messageTypeProcessed);

            var processingTime = e.CompletedAt - e.StartedAt;

            var @event = new DurationEvent(processingTime, messageTypeProcessed);

            probe.Record(ref @event);

            return Task.CompletedTask;
        });

    public const string ProcessingTime = "Processing Time";
}