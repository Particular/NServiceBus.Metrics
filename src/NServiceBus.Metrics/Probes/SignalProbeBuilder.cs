using System;
using System.Reflection;
using NServiceBus;
using NServiceBus.Metrics;

abstract class SignalProbeBuilder
{
    protected abstract void WireUp(SignalProbe probe);

    public SignalProbe Build()
    {
        var probe = GetProbe();

        WireUp(probe);

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