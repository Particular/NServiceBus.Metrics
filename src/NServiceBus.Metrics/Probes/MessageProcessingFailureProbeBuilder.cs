using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

[ProbeProperties("# of messages successfully processed / sec", "Message processing succeeded.")]
class MessageProcessingFailureProbeBuilder : SignalProbeBuilder
{
    public MessageProcessingFailureProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior)
    {
        this.behavior = behavior;
    }

    protected override void WireUp(FeatureConfigurationContext context, SignalProbe probe)
    {
        behavior.ProcessingFailure = probe;
    }

    ReceivePerformanceDiagnosticsBehavior behavior;
}