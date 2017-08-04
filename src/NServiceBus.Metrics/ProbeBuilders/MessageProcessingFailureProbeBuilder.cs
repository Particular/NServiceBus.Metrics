[ProbeProperties("nservicebus_failure_total", "# of msgs failures / sec", "The current number of failed processed messages by the transport per second.")]
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