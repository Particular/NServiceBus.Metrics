using System.Reflection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;



abstract class SignalProbeBuilder
{
    protected abstract void WireUp(FeatureConfigurationContext context, SignalProbe probe);

    public SignalProbe Build(FeatureConfigurationContext context)
    {
        var probe = GetProbe();

        WireUp(context, probe);

        return probe;
    }

    SignalProbe GetProbe()
    {
        var attr = GetType().GetCustomAttribute<ProbePropertiesAttribute>();

        return new SignalProbe(attr.Name, attr.Description);
    }
}

[ProbeProperties("# of messages pulled from the input queue / sec", "Message pulled from the queue.")]
class MessagePulledFromQueueProbeBuilder : SignalProbeBuilder
{
    public MessagePulledFromQueueProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior)
    {
        this.behavior = behavior;
    }

    protected override void WireUp(FeatureConfigurationContext context, SignalProbe probe)
    {
        behavior.MessagePulledFromQueue = probe;
    }

    ReceivePerformanceDiagnosticsBehavior behavior;
}

[ProbeProperties("# of message failures / sec", "Message processing failed.")]
class MessageProcessingSuccessProbeBuilder : SignalProbeBuilder
{
    public MessageProcessingSuccessProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior)
    {
        this.behavior = behavior;
    }

    protected override void WireUp(FeatureConfigurationContext context, SignalProbe probe)
    {
        behavior.ProcessingSuccess = probe;
    }

    ReceivePerformanceDiagnosticsBehavior behavior;
}

[ProbeProperties("# of messages successfully processed / sec", "Message processing succeeded.")]
class MessageProcessingFailureProbeBuilder : SignalProbeBuilder
{
    public MessageProcessingFailureProbeBuilder(ReceivePerformanceDiagnosticsBehavior behavior)
    {
        this.behavior = behavior;
    }

    protected override void WireUp(FeatureConfigurationContext context, SignalProbe probe)
    {
        behavior.ProcessingFailure = probe;
    }

    ReceivePerformanceDiagnosticsBehavior behavior;
}