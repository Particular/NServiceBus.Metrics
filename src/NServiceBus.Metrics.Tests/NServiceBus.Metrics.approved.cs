﻿[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute(@"NServiceBus.Metrics.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001007f16e21368ff041183fab592d9e8ed37e7be355e93323147a1d29983d6e591b04282e4da0c9e18bd901e112c0033925eb7d7872c2f1706655891c5c9d57297994f707d16ee9a8f40d978f064ee1ffc73c0db3f4712691b23bf596f75130f4ec978cf78757ec034625a5f27e6bb50c618931ea49f6f628fd74271c32959efb1c5")]
[assembly: System.Runtime.InteropServices.ComVisibleAttribute(true)]
[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.5.2", FrameworkDisplayName=".NET Framework 4.5.2")]

namespace NServiceBus
{
    
    public interface IDurationProbe : NServiceBus.IProbe
    {
        void Register(System.Action<System.TimeSpan> observer);
    }
    public interface IProbe
    {
        string Description { get; }
        string Name { get; }
    }
    public interface ISignalProbe : NServiceBus.IProbe
    {
        void Register(System.Action observer);
    }
    public class static MetricsConfigurationExtensions
    {
        public static NServiceBus.MetricsOptions EnableMetrics(this NServiceBus.Settings.SettingsHolder settings) { }
        public static NServiceBus.MetricsOptions EnableMetrics(this NServiceBus.EndpointConfiguration endpointConfiguration) { }
    }
    public class MetricsOptions
    {
        public MetricsOptions() { }
        public void EnableCustomReport(System.Func<string, System.Threading.Tasks.Task> func, System.TimeSpan interval) { }
        public void EnableLogTracing(System.TimeSpan interval, NServiceBus.Logging.LogLevel logLevel = 0) { }
        public void EnableMetricTracing(System.TimeSpan interval) { }
        public void RegisterObservers(System.Action<NServiceBus.ProbeContext> register) { }
        [System.ObsoleteAttribute("Not for public use.")]
        public void SendMetricDataToServiceControl(string serviceControlMetricsAddress, System.TimeSpan interval) { }
    }
    public class ProbeContext
    {
        public ProbeContext(System.Collections.Generic.IReadOnlyCollection<NServiceBus.IDurationProbe> durations, System.Collections.Generic.IReadOnlyCollection<NServiceBus.ISignalProbe> signals) { }
        public System.Collections.Generic.IReadOnlyCollection<NServiceBus.IDurationProbe> Durations { get; }
        public System.Collections.Generic.IReadOnlyCollection<NServiceBus.ISignalProbe> Signals { get; }
    }
}