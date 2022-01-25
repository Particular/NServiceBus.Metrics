#pragma warning disable 1591
namespace NServiceBus
{
    using System;

    public static partial class MetricsConfigurationExtensions
    {
        [ObsoleteEx(
            ReplacementTypeOrMember = "EnableOptions(this EndpointConfiguration endpointConfiguration)",
            TreatAsErrorFromVersion = "4",
            RemoveInVersion = "5"
            )]
        public static MetricsOptions EnableMetrics(this Settings.SettingsHolder settings)
            => throw new NotImplementedException();
    }
}
#pragma warning restore 1591