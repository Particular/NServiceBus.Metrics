using NServiceBus.Metrics;

[ProbeProperties(Probes.MessagePulled,"# of msgs pulled from the input queue /sec")]
class MessagePulledFromQueueProbeBuilder : SignalProbeBuilder
{
    public MessagePulledFromQueueProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior)
    {
        this.behavior = behavior;
    }

    protected override void WireUp(SignalProbe probe)
    {
        behavior.MessagePulledFromQueue = probe;
    }

    readonly ReceivePerformanceDiagnosticsBehavior behavior;
}