namespace NServiceBus.Metrics
{
    using System;
    using System.Threading.Tasks;
    using Features;

    [ProbeProperties("Critical Time", "Time between message sent till end of processing.")]
    class CriticalTimeProbeBuilder : DurationProbeBuilder
    {
        static readonly Task<int> CompletedTask = Task.FromResult(0);

        protected override void WireUp(FeatureConfigurationContext context, DurationProbe probe)
        {
            context.Pipeline.OnReceivePipelineCompleted(e =>
            {
                DateTime timeSent;
                if (e.TryGetTimeSent(out timeSent))
                {
                    var endToEndTime = e.CompletedAt - timeSent;

                    probe.Record(endToEndTime);
                }

                return CompletedTask;
            });
        }
    }
}