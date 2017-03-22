﻿namespace NServiceBus
{
    using Configuration.AdvanceExtensibility;
    using Metrics;

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
            var metricsConfiguration = new MetricsOptions();
            endpointConfiguration.GetSettings().Set<MetricsOptions>(metricsConfiguration);

            endpointConfiguration.EnableFeature<MetricsFeature>();

            return metricsConfiguration;
        }
    }
}