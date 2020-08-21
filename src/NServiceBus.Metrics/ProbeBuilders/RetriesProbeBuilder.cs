namespace NServiceBus.Metrics.ProbeBuilders
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Features;

    [ProbeProperties(Retries, "A message has been scheduled for retry (FLR or SLR)")]
    class RetriesProbeBuilder : SignalProbeBuilder
    {
        public const string Retries = "Retries";

        public RetriesProbeBuilder(FeatureConfigurationContext context)
        {
            recoverability = context.Settings.Get<RecoverabilitySettings>();
        }

        protected override void WireUp(SignalProbe probe)
        {
            recoverability.Immediate(s => s.OnMessageBeingRetried(retry => Signal(retry.Headers, probe)));
            recoverability.Delayed(s => s.OnMessageBeingRetried(retry => Signal(retry.Headers, probe)));
        }

        static Task Signal(Dictionary<string, string> messageHeaders, SignalProbe probe)
        {
            messageHeaders.TryGetMessageType(out var messageType);

            var @event = new SignalEvent(messageType);
            probe.Signal(ref @event);
            return Task.CompletedTask;
        }

        RecoverabilitySettings recoverability;
    }
}