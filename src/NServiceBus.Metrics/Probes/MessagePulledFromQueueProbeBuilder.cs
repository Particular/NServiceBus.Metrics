using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;

[ProbeProperties("# of messages pulled from the input queue / sec", "Message pulled from the queue.")]
class MessagePulledFromQueueProbeBuilder : SignalProbeBuilder
{
    public MessagePulledFromQueueProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior)
    {
        this.behavior = behavior;
    }

    protected override void WireUp(FeatureConfigurationContext context, SignalProbe probe)
    {
        behavior.MessagePulledFromQueue = probe;
    }

    ReceivePerformanceDiagnosticsBehavior behavior;
}