namespace NServiceBus.Metrics
{
    using Features;

    /// <summary>
    /// Exposes metric builder registration on the feature configuration context.
    /// </summary>
    static class FeatureConfigurationContextExtensions
    {
        /// <summary>
        /// Registers a metric builder.
        /// </summary>
        public static void RegisterMetricBuilder(this FeatureConfigurationContext context, MetricBuilder builder)
        {
            context.Settings.Get<MetricsRegistry>().RegisterMetricBuilder(builder);
        }
    }
}