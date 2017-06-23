namespace NServiceBus.Metrics
{
    using System.Threading.Tasks;
    using Features;

    [ProbeProperties("Processing Time", "The time it took to successfully process a message.")]
    class ProcessingTimeProbeBuilder : DurationProbeBuilder
    {
        static readonly Task<int> CompletedTask = Task.FromResult(0);

        protected override void WireUp(FeatureConfigurationContext context, DurationProbe probe)
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
    }
}