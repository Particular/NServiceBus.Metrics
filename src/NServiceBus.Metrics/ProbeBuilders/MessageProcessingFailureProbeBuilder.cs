[ProbeProperties("# of messages successfully processed / sec", "Message processing succeeded.")]
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