namespace NServiceBus.Metrics
{
    using System;
    using System.Reflection;
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