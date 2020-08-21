namespace NServiceBus.Metrics.ProbeBuilders
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Threading.Tasks;
    using Features;
    using Settings;

    [ProbeProperties(Retries, "A message has been scheduled for retry (FLR or SLR)")]
    class RetriesProbeBuilder : SignalProbeBuilder
    {
        public const string Retries = "Retries";

        public RetriesProbeBuilder(FeatureConfigurationContext context)
        {
            immediateRetriesSettings = (ImmediateRetriesSettings) Activator.CreateInstance(
                typeof(ImmediateRetriesSettings),
                flags,
                null,
                new object[]
                {
                    (SettingsHolder) context.Settings
                },
                CultureInfo.InvariantCulture);
            delayedRetriesSettings = (DelayedRetriesSettings) Activator.CreateInstance(typeof(DelayedRetriesSettings),
                flags,
                null,
                new object[]
                {
                    (SettingsHolder) context.Settings
                },
                CultureInfo.InvariantCulture);
        }

        protected override void WireUp(SignalProbe probe)
        {
            immediateRetriesSettings.OnMessageBeingRetried(retry => Signal(retry.Headers, probe));
            delayedRetriesSettings.OnMessageBeingRetried(retry => Signal(retry.Headers, probe));
        }

        static Task Signal(Dictionary<string, string> messageHeaders, SignalProbe probe)
        {
            messageHeaders.TryGetMessageType(out var messageType);

            var @event = new SignalEvent(messageType);
            probe.Signal(ref @event);
            return Task.CompletedTask;
        }

        ImmediateRetriesSettings immediateRetriesSettings;
        DelayedRetriesSettings delayedRetriesSettings;

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
    }
}