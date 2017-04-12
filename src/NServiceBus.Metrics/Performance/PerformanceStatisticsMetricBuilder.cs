using Metrics;
using NServiceBus.Features;
using NServiceBus.Metrics;

class PerformanceStatisticsMetricBuilder : MetricBuilder
{
    public override void WireUp(FeatureConfigurationContext featureConfigurationContext)
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

    [Meter("# of messages pulled from the input queue / sec", "Messages")]
    Meter messagesPulledFromQueueMeter = default(Meter);

    [Meter("# of message failures / sec", "Messages")]
    Meter failureRateMeter = default(Meter);

    [Meter("# of messages successfully processed / sec", "Messages")]
    Meter successRateMeter = default(Meter);
}