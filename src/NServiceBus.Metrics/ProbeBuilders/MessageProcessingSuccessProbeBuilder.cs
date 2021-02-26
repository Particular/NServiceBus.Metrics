using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;

[ProbeProperties("# of msgs successfully processed / sec", "The current number of messages processed successfully by the transport per second.")]
class MessageProcessingSuccessProbeBuilder : SignalProbeBuilder
{
    public MessageProcessingSuccessProbeBuilder(FeatureConfigurationContext context)
    {
        this.context = context;
    }

    protected override void WireUp(SignalProbe probe)
    {
        context.OnReceiveCompleted((e, _) =>
        {
            if (!e.OnMessageFailed && e.WasAcknowledged)
            {
                e.TryGetMessageType(out var messageType);

                var @event = new SignalEvent(messageType);

                probe.Signal(ref @event);
            }

            return Task.CompletedTask;
        });
    }

    readonly FeatureConfigurationContext context;
}