namespace NServiceBus.Metrics.ProbeBuilders
{
    using Features;

    [ProbeProperties("Retries", "A message has been scheduled for retry (FLR or SLR)")]
    class RetriesProbeBuilder : SignalProbeBuilder
    {
        Notifications notifications;

        public RetriesProbeBuilder(FeatureConfigurationContext context)
        {
            notifications = context.Settings.Get<Notifications>();
        }

        protected override void WireUp(SignalProbe probe)
        {
            notifications.Errors.MessageHasFailedAnImmediateRetryAttempt += (sender, message) => probe.Signal();
            notifications.Errors.MessageHasBeenSentToDelayedRetries += (sender, message) => probe.Signal();
        }
    }
}