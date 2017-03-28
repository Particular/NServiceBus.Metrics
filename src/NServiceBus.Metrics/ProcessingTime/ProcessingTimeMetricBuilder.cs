using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;

class ProcessingTimeMetricBuilder : IMetricBuilder
{
    public void WireUp(FeatureConfigurationContext featureConfigurationContext, MetricsContext metricsContext, Unit messagesUnit)
    {
        var processingTimeTimer = metricsContext.Timer("Processing Time", messagesUnit);

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