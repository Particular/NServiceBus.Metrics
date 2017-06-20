using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

class ProcessingTimeProbeBuilder : ProbeBuilder
{
    public override Probe[] WireUp(FeatureConfigurationContext featureConfigurationContext)
    {
        var probe = new Probe("Processing Time", MeasurementValueType.Time, "The time it took to successfully process a message.");

        featureConfigurationContext.Pipeline.OnReceivePipelineCompleted(e =>
        {
            var processingTimeInMilliseconds = ProcessingTimeCalculator.Calculate(e.StartedAt, e.CompletedAt).TotalMilliseconds;

            string messageTypeProcessed;
            e.TryGetMessageType(out messageTypeProcessed);

            probe.Record((long) processingTimeInMilliseconds);

            return Task.FromResult(0);
        });

        return new[]
        {
            probe
        };
    }
}