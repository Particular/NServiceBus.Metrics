namespace NServiceBus.Metrics.ProbeBuilders
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    [ProbeProperties(Retries, "A message has been scheduled for retry (FLR or SLR)")]
    class RetriesProbeBuilder(MetricsOptions options) : SignalProbeBuilder
    {
        protected override void WireUp(SignalProbe probe)
        {
            options.Immediate = (retry, token) => Signal(retry.Headers, probe, token);
            options.Delayed = (retry, token) => Signal(retry.Headers, probe, token);
        }

        static Task Signal(Dictionary<string, string> messageHeaders, SignalProbe probe, CancellationToken cancellationToken)
        {
            messageHeaders.TryGetMessageType(out var messageType);

            var @event = new SignalEvent(messageType);
            probe.Signal(ref @event);
            return Task.CompletedTask;
        }

        public const string Retries = "Retries";
    }
}