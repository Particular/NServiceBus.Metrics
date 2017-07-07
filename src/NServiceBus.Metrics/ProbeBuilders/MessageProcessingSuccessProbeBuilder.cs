[ProbeProperties("# of msgs successfully processed / sec", "The current number of messages processed successfully by the transport per second.")]
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