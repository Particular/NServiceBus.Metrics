using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;

class ProcessingTimeMetricBuilder : IMetricBuilder
{
    Timer processingTimeTimer;

    public void Define(MetricsContext metricsContext)
    {
        processingTimeTimer = metricsContext.Timer("Processing Time", Unit.Custom("Messages"));
    }

    public void WireUp(FeatureConfigurationContext featureConfigurationContext)
    {
        featureConfigurationContext.Pipeline.OnReceivePipelineCompleted(e =>
        {
            var processingTimeInMilliseconds = ProcessingTimeCalculator.Calculate(e.StartedAt, e.CompletedAt).TotalMilliseconds;

            string messageTypeProcessed;
            e.TryGetMessageType(out messageTypeProcessed);

            processingTimeTimer.Record((long)processingTimeInMilliseconds, TimeUnit.Milliseconds, messageTypeProcessed);

            return Task.FromResult(0);
        });
    }
}