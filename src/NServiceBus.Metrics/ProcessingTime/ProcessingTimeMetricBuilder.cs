using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

class ProcessingTimeMetricBuilder : MetricBuilder
{
    public override void WireUp(FeatureConfigurationContext featureConfigurationContext)
    {
        featureConfigurationContext.Pipeline.OnReceivePipelineCompleted(e =>
        {
            var processingTimeInMilliseconds = ProcessingTimeCalculator.Calculate(e.StartedAt, e.CompletedAt).TotalMilliseconds;

            string messageTypeProcessed;
            e.TryGetMessageType(out messageTypeProcessed);

            processingTimeTimer.Record((long) processingTimeInMilliseconds, TimeUnit.Milliseconds);

            return Task.FromResult(0);
        });
    }

    [Timer("Processing Time", "Messages", "The time it took to successfully process a message.")]
    Timer processingTimeTimer = default(Timer);
}