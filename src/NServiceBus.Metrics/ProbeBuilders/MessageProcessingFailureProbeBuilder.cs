[ProbeProperties("# of msgs failures / sec", "The current number of failed processed messages by the transport per second.")]
class MessageProcessingFailureProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior) : SignalProbeBuilder
{
    protected override void WireUp(SignalProbe probe) => behavior.ProcessingFailure = probe;
}