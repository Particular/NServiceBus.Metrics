using System;
using System.Reflection;

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

        if (attribute == null)
        {
            var exceptionMessage = $"The type '{GetType()}' is not annotated with required '{typeof(ProbePropertiesAttribute).Name}'. Add this attribute to provide necessary metadata for the probe.";

            throw new Exception(exceptionMessage);
        }

        return new DurationProbe(attribute.Id, attribute.Name);
    }
}