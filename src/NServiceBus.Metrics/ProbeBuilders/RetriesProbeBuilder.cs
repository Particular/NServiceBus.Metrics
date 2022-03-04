namespace NServiceBus.Metrics.ProbeBuilders
{
    using System.Collections.Generic;
    using Features;

    [ProbeProperties(Retries, "A message has been scheduled for retry (FLR or SLR)")]
    class RetriesProbeBuilder : SignalProbeBuilder
    {
        public const string Retries = "Retries";

        public RetriesProbeBuilder(FeatureConfigurationContext context)
        {
            notifications = context.Settings.Get<Notifications>();
        }

        protected override void WireUp(SignalProbe probe)
        {
            var errors = notifications.Errors;
#pragma warning disable CS0618 // Type or member is obsolete
            errors.MessageHasFailedAnImmediateRetryAttempt += (sender, message) => Signal(message.Headers, probe);
            errors.MessageHasBeenSentToDelayedRetries += (sender, message) => Signal(message.Headers, probe);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        static void Signal(Dictionary<string, string> messageHeaders, SignalProbe probe)
        {
            messageHeaders.TryGetMessageType(out var messageType);

            var @event = new SignalEvent(messageType);
            probe.Signal(ref @event);
        }

        Notifications notifications;
    }
}