using NServiceBus.Metrics;

class MessageProcessingFailureProbeBuilder : SignalProbeBuilder
{
    public MessageProcessingFailureProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior)
    {
        this.behavior = behavior;
    }

    protected override void WireUp(SignalProbe probe)
    {
        behavior.ProcessingFailure = probe;
    }

    protected override string ProbeId => Probes.MessageFailed;

    readonly ReceivePerformanceDiagnosticsBehavior behavior;
}