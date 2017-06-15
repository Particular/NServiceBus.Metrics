﻿namespace NServiceBus
{
    using System;
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
        /// <param name="reportingInterval">Interval between metrics report generation.</param>
        /// <returns>An object containing configuration options for the Metrics feature.</returns>
        public static MetricsOptions EnableMetrics(this SettingsHolder settings, TimeSpan? reportingInterval = null)
        {
            var options = settings.GetOrCreate<MetricsOptions>();

            if (reportingInterval.HasValue)
            {
                Guard.AgainstNegativeAndZero(nameof(reportingInterval), reportingInterval);

                options.ReportInterval(reportingInterval.Value);
            }

            settings.Set(typeof(MetricsFeature).FullName, FeatureState.Enabled);

            return options;
        }

        /// <summary>
        /// Enables the Metrics feature.
        /// </summary>
        /// <param name="endpointConfiguration">The endpoint configuration to enable the metrics feature on.</param>
        /// <param name="reportingInterval">Interval between metrics report generation.</param>
        /// <returns>An object containing configuration options for the Metrics feature.</returns>
        public static MetricsOptions EnableMetrics(this EndpointConfiguration endpointConfiguration, TimeSpan? reportingInterval = null)
        {
            Guard.AgainstNull(nameof(endpointConfiguration), endpointConfiguration);

            return EnableMetrics(endpointConfiguration.GetSettings(), reportingInterval);
        }
    }
}
