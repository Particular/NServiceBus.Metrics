namespace NServiceBus.Metrics.ProbeBuilders
{
    using Features;

    [ProbeProperties(Probes.RetryOccurred, Retries)]
    class RetriesProbeBuilder : SignalProbeBuilder
    {
        public const string Retries = "Retries";

        public RetriesProbeBuilder(FeatureConfigurationContext context)
        {
            notifications = context.Settings.Get<Notifications>();
        }

        protected override void WireUp(SignalProbe probe)
        {
            notifications.Errors.MessageHasFailedAnImmediateRetryAttempt += (sender, message) => probe.Signal();
            notifications.Errors.MessageHasBeenSentToDelayedRetries += (sender, message) => probe.Signal();
        }

        readonly Notifications notifications;
    }
}