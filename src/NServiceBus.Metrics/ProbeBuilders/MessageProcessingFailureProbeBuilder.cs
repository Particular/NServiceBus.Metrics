using NServiceBus.Metrics;

[ProbeProperties(Probes.MessageFailed, "# of msgs failures / sec")]
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

    readonly ReceivePerformanceDiagnosticsBehavior behavior;
}