namespace NServiceBus.Metrics.ProbeBuilders
{
    using Features;

    class RetriesProbeBuilder : SignalProbeBuilder
    {
        public RetriesProbeBuilder(FeatureConfigurationContext context)
        {
            notifications = context.Settings.Get<Notifications>();
        }

        protected override void WireUp(SignalProbe probe)
        {
            notifications.Errors.MessageHasFailedAnImmediateRetryAttempt += (sender, message) => probe.Signal();
            notifications.Errors.MessageHasBeenSentToDelayedRetries += (sender, message) => probe.Signal();
        }

        protected override string ProbeId => Probes.RetryOccurred;

        readonly Notifications notifications;
    }
}