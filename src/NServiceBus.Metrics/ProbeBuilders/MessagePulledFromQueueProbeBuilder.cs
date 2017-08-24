using NServiceBus.Metrics;

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

    protected override string ProbeId => Probes.MessagePulled;

    readonly ReceivePerformanceDiagnosticsBehavior behavior;
}