[ProbeProperties("nservicebus_fetched_total", "# of msgs pulled from the input queue /sec", "The current number of messages pulled from the input queue by the transport per second.")]
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