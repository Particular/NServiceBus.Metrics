namespace NServiceBus
{
    using Configuration.AdvancedExtensibility;
    using Features;

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
            settings.Set(typeof(MetricsFeature).FullName, FeatureState.Enabled);

            // any ideas how to get rid of the closure?
            endpointConfiguration.Recoverability().Immediate(c => c.OnMessageBeingRetried(m => options.Immediate(m)));
            endpointConfiguration.Recoverability().Delayed(c => c.OnMessageBeingRetried(m => options.Delayed(m)));

            return options;
        }
    }
}