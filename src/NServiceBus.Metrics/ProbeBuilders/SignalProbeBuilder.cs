using System;
using System.Reflection;

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
            var exceptionMessage = $"The type '{GetType()}' is not annotated with required '{nameof(ProbePropertiesAttribute)}'. This attribute has to be added to provide necessary metadata for the probe.";

            throw new Exception(exceptionMessage);
        }

        return new SignalProbe(attribute.Name, attribute.Description);
    }
}