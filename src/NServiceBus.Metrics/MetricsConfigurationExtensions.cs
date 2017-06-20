namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;
    using Features;
    using Settings;

    /// <summary>
    /// Extends Endpoint Configuration to provide Metric options
    /// </summary>
    public static class MetricsConfigurationExtensions
    {
        /// <summary>
        /// Enables the Metrics feature.
        /// </summary>
        /// <param name="settings">The settings to enable the metrics feature on.</param>
        /// <returns>An object containing configuration options for the Metrics feature.</returns>
        static MetricsOptions EnableMetrics(this SettingsHolder settings)
        {
            var options = settings.GetOrCreate<MetricsOptions>();

            settings.Set(typeof(MetricsFeature).FullName, FeatureState.Enabled);

            return options;
        }

        /// <summary>
        /// Enables the Metrics feature.
        /// </summary>
        /// <param name="endpointConfiguration">The endpoint configuration to enable the metrics feature on.</param>
        /// <returns>An object containing configuration options for the Metrics feature.</returns>
        public static MetricsOptions EnableMetrics(this EndpointConfiguration endpointConfiguration)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);

            return EnableMetrics(endpointConfiguration.GetSettings());
        }
    }
}
