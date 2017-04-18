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

    [Meter("# of msgs pulled from the input queue / sec", "Messages", "The current number of messages pulled from the input queue by the transport per second.")]
    Meter messagesPulledFromQueueMeter = default(Meter);

    [Meter("# of msgs failures / sec", "Messages", "The current number of failed processed messages by the transport per second.")]
    Meter failureRateMeter = default(Meter);

    [Meter("# of msgs successfully processed / sec", "Messages", "The current number of messages processed successfully by the transport per second.")]
    Meter successRateMeter = default(Meter);
}