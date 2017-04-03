using Metrics;
using NServiceBus.Features;

class PerformanceStatisticsMetricBuilder : IMetricBuilder
{
    public void WireUp(FeatureConfigurationContext featureConfigurationContext, MetricsContext metricsContext, Unit messagesUnit)
    {
        var messagesPulledFromQueueMeter = metricsContext.Meter("# of messages pulled from the input queue / sec", messagesUnit);
        var failureRateMeter = metricsContext.Meter("# of message failures / sec", messagesUnit);
        var successRateMeter = metricsContext.Meter("# of messages successfully processed / sec", messagesUnit);

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