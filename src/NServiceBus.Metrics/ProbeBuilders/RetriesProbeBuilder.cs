namespace NServiceBus.Metrics.ProbeBuilders
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [ProbeProperties(Retries, "A message has been scheduled for retry (FLR or SLR)")]
    class RetriesProbeBuilder : SignalProbeBuilder
    {
        public RetriesProbeBuilder(MetricsOptions options)
        {
            this.options = options;
        }

        protected override void WireUp(SignalProbe probe)
        {
            options.Immediate = retry => Signal(retry.Headers, probe);
            options.Delayed = retry => Signal(retry.Headers, probe);
        }

        static Task Signal(Dictionary<string, string> messageHeaders, SignalProbe probe)
        {
            messageHeaders.TryGetMessageType(out var messageType);

            var @event = new SignalEvent(messageType);
            probe.Signal(ref @event);
            return Task.CompletedTask;
        }

        MetricsOptions options;
        public const string Retries = "Retries";
    }
}