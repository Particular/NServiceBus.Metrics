using Metrics;
using NServiceBus.Features;

class PerformanceStatisticsMetricBuilder : IMetricBuilder
{
    Meter messagesPulledFromQueueMeter;
    Meter failureRateMeter;
    Meter successRateMeter;

    public void Define(MetricsContext metricsContext)
    {
        messagesPulledFromQueueMeter = metricsContext.Meter("# of messages pulled from the input queue / sec", Unit.Custom("Messages"));
        failureRateMeter = metricsContext.Meter("# of message failures / sec", Unit.Custom("Messages"));
        successRateMeter = metricsContext.Meter("# of messages successfully processed / sec", Unit.Custom("Messages"));
    }

    public void WireUp(FeatureConfigurationContext featureConfigurationContext)
    {
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