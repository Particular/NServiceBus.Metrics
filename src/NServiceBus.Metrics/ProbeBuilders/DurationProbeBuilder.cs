namespace NServiceBus.Metrics
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a custom metric.
    /// </summary>
    abstract class DurationProbeBuilder
    {
        protected abstract void WireUp(DurationProbe probe);

        public DurationProbe Build()
        {
            var probe = GetProbe();

            WireUp(probe);

            return probe;
        }

        DurationProbe GetProbe()
        {
            var attribute = GetType().GetCustomAttribute<ProbePropertiesAttribute>();

            if(attribute == null)
            {
                var exceptionMessage = $"The type '{GetType()}' is not annotated with required '{typeof(ProbePropertiesAttribute).Name}'. This attribute has to be added to provide necessary metadata for the probe.";

                throw new Exception(exceptionMessage);
            }

            return new DurationProbe(attribute.Name, attribute.Description);
        }

        protected static readonly Task<int> CompletedTask = Task.FromResult(0);
    }
}