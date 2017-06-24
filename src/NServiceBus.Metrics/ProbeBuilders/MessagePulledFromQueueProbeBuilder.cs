[ProbeProperties(ProbeType.Signal, "# of messages pulled from the input queue / sec", "Message pulled from the queue.")]
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