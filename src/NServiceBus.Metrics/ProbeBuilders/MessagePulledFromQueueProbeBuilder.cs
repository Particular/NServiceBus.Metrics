[ProbeProperties("# of msgs pulled from the input queue /sec", "The current number of messages pulled from the input queue by the transport per second.")]
class MessagePulledFromQueueProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior) : SignalProbeBuilder
{
    protected override void WireUp(SignalProbe probe) => behavior.MessagePulledFromQueue = probe;
}