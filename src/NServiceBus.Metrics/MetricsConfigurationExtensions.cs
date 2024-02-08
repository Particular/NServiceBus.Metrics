namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Features;
    using Metrics;

    /// <summary>
    /// Extends Endpoint Configuration to provide Metric options
    /// </summary>
    public static class MetricsConfigurationExtensions
    {
        /// <summary>
        /// Enables the Metrics feature.
        /// </summary>
        /// <param name="endpointConfiguration">The endpoint configuration to enable the metrics feature on.</param>
        /// <returns>An object containing configuration options for the Metrics feature.</returns>
        public static MetricsOptions EnableMetrics(this EndpointConfiguration endpointConfiguration)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);

            var settings = endpointConfiguration.GetSettings();
            var options = settings.GetOrCreate<MetricsOptions>();

            settings.EnableFeatureByDefault<MetricsFeature>();

            endpointConfiguration.Recoverability().Immediate(c => c.OnMessageBeingRetried((m, ct) => options.Immediate(m, ct)));
            endpointConfiguration.Recoverability().Delayed(c => c.OnMessageBeingRetried((m, ct) => options.Delayed(m, ct)));

            return options;
        }
    }
}
