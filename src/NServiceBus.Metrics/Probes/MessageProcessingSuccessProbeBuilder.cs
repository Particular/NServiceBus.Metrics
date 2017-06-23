using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

[ProbeProperties("# of message failures / sec", "Message processing failed.")]
class MessageProcessingSuccessProbeBuilder : SignalProbeBuilder
{
    public MessageProcessingSuccessProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior)
    {
        this.behavior = behavior;
    }

    protected override void WireUp(FeatureConfigurationContext context, SignalProbe probe)
    {
        behavior.ProcessingSuccess = probe;
    }

    ReceivePerformanceDiagnosticsBehavior behavior;
}