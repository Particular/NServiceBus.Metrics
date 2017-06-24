[ProbeProperties(ProbeType.Signal, "# of message failures / sec", "Message processing failed.")]
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