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
        var attr = GetType().GetCustomAttribute<ProbePropertiesAttribute>();

        return new SignalProbe(attr.Name, attr.Description);
    }
}