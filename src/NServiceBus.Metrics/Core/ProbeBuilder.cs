namespace NServiceBus.Metrics
{
    using Features;

    /// <summary>
    /// Defines a custom metric.
    /// </summary>
    abstract class ProbeBuilder
    {
        /// <summary>
        /// Called to Wire up the metric facades with metric updating code.
        /// </summary>
        public abstract Probe[] WireUp(FeatureConfigurationContext featureConfigurationContext);
    }
}