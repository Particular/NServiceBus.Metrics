using NServiceBus.Metrics;

[ProbeProperties(Probes.MessageProcessed, "# of msgs successfully processed / sec")]
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

    readonly ReceivePerformanceDiagnosticsBehavior behavior;
}