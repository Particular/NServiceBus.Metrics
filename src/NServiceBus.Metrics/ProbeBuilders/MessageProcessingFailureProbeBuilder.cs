using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;

[ProbeProperties("# of msgs failures / sec", "The current number of failed processed messages by the transport per second.")]
class MessageProcessingFailureProbeBuilder : SignalProbeBuilder
{
    public MessageProcessingFailureProbeBuilder(FeatureConfigurationContext context)
    {
        this.context = context;
    }

    protected override void WireUp(SignalProbe probe)
    {
        context.OnReceiveCompleted((e, _) =>
        {
            if (e.OnMessageFailed || !e.WasAcknowledged)
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