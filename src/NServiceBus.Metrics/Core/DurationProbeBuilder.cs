namespace NServiceBus.Metrics
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using Features;

    /// <summary>
    /// Defines a custom metric.
    /// </summary>
    abstract class DurationProbeBuilder
    {
        protected abstract void WireUp(FeatureConfigurationContext context, DurationProbe probe);

        public DurationProbe Build(FeatureConfigurationContext context)
        {
            var probe = GetProbe();

            WireUp(context, probe);

            return probe;
        }

        DurationProbe GetProbe()
        {
            var attr = GetType().GetCustomAttribute<ProbePropertiesAttribute>();

            return new DurationProbe(attr.Name, attr.Description);
        }
    }

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

    [ProbeProperties("Processing Time", "The time it took to successfully process a message.")]
    class ProcessingTimeProbeBuilder : DurationProbeBuilder
    {
        static readonly Task<int> CompletedTask = Task.FromResult(0);

        protected override void WireUp(FeatureConfigurationContext context, DurationProbe probe)
        {
            context.Pipeline.OnReceivePipelineCompleted(e =>
            {
                string messageTypeProcessed;
                e.TryGetMessageType(out messageTypeProcessed);

                var processingTime = e.CompletedAt - e.StartedAt;

                probe.Record(processingTime);

                return CompletedTask;
            });
        }
    }

    class ProbePropertiesAttribute : Attribute
    {
        public ProbePropertiesAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public readonly string Name;
        public readonly string Description;
    }
}