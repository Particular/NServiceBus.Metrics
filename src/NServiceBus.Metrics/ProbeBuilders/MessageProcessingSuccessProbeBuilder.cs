using NServiceBus.Metrics;

class MessageProcessingSuccessProbeBuilder : SignalProbeBuilder
{
    public MessageProcessingSuccessProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior)
    {
        this.behavior = behavior;
    }

    protected override void WireUp(SignalProbe probe)
    {
        behavior.ProcessingSuccess = probe;
    }

    protected override string ProbeId => Probes.MessageProcessed;

    readonly ReceivePerformanceDiagnosticsBehavior behavior;
}