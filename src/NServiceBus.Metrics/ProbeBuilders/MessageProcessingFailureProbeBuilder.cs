using NServiceBus;
using NServiceBus.Metrics;

[ProbeProperties(ProbeType.Signal, "# of messages successfully processed / sec", "Message processing succeeded.")]
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

    ReceivePerformanceDiagnosticsBehavior behavior;
}