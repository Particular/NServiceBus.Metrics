using Metrics;
using NServiceBus.Features;

class PerformanceStatisticsMetricBuilder : IMetricBuilder
{
    public void WireUp(FeatureConfigurationContext featureConfigurationContext, MetricsContext metricsContext, Unit messagesUnit)
    {
        var messagesPulledFromQueueMeter = metricsContext.Meter("Messages Pulled from Queue", messagesUnit);
        var failureRateMeter = metricsContext.Meter("Message Processing Failures", messagesUnit);
        var successRateMeter = metricsContext.Meter("Messages Successfully Processed", messagesUnit);

        var performanceDiagnosticsBehavior = new ReceivePerformanceDiagnosticsBehavior(
            messagesPulledFromQueueMeter,
            failureRateMeter,
            successRateMeter
        );

        featureConfigurationContext.Pipeline.Register(
            "NServiceBus.Metrics.ReceivePerformanceDiagnosticsBehavior",
            performanceDiagnosticsBehavior,
            "Provides various performance counters for receive statistics"
        );
    }
}