[ProbeProperties("# of msgs successfully processed / sec", "The current number of messages processed successfully by the transport per second.")]
class MessageProcessingSuccessProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior) : SignalProbeBuilder
{
    protected override void WireUp(SignalProbe probe) => behavior.ProcessingSuccess = probe;
}