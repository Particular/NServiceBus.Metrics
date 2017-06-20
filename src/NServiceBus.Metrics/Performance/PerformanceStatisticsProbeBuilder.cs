using NServiceBus.Features;
using NServiceBus.Metrics;

class PerformanceStatisticsProbeBuilder : ProbeBuilder
{
    public override Probe[] WireUp(FeatureConfigurationContext featureConfigurationContext)
    {
        var messagesPulledFromQueue = new Probe("# of messages pulled from the input queue / sec", MeasurementValueType.Count, string.Empty);
        var processingFailure = new Probe("# of message failures / sec", MeasurementValueType.Count, string.Empty);
        var processingSuccess = new Probe("# of messages successfully processed / sec", MeasurementValueType.Count, string.Empty);

        var performanceDiagnosticsBehavior = new ReceivePerformanceDiagnosticsBehavior(
            messagesPulledFromQueue,
            processingFailure,
            processingSuccess
        );

        featureConfigurationContext.Pipeline.Register(
            "NServiceBus.Metrics.ReceivePerformanceDiagnosticsBehavior",
            performanceDiagnosticsBehavior,
            "Provides various performance counters for receive statistics"
        );

        return new[]
        {
            messagesPulledFromQueue,
            processingFailure,
            processingSuccess
        };
    }
}