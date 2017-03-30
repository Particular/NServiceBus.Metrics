namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;

    /// <summary>
    /// Extends Endpoint Configuration to provide Metric options
    /// </summary>
    public static class MetricsConfigurationExtensions
    {
        /// <summary>
        /// Enables the Metrics feature
        /// </summary>
        /// <param name="endpointConfiguration">The endpoint configuration to enable the metrics feature on</param>
        /// <returns>An object containing configuration options for the Metrics feature</returns>
        public static MetricsOptions EnableMetrics(this EndpointConfiguration endpointConfiguration)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);

            var options = endpointConfiguration.GetSettings().GetOrCreate<MetricsOptions>();
            endpointConfiguration.EnableFeature<MetricsFeature>();
            return options;
        }
    }
}