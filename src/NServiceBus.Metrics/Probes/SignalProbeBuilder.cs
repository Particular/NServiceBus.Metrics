using System;
using System.Reflection;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics;



abstract class SignalProbeBuilder
{
    protected abstract void WireUp(FeatureConfigurationContext context, SignalProbe probe);

    public SignalProbe Build(FeatureConfigurationContext context)
    {
        var probe = GetProbe();

        WireUp(context, probe);

        return probe;
    }

    SignalProbe GetProbe()
    {
        var attribute = GetType().GetCustomAttribute<ProbePropertiesAttribute>();

        if (attribute == null)
        {
            var exceptionMessage = $"The type '{GetType()}' is not annotated with required '{typeof(ProbePropertiesAttribute).Name}'. This attribute has to be added to provide necessary metadata for the probe.";

            throw new Exception(exceptionMessage);
        }

        return new SignalProbe(attribute.Name, attribute.Description);
    }
}