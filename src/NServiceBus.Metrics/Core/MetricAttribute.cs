namespace NServiceBus.Metrics
{
    using System;
    using global::Metrics;

    /// <summary>
    /// Base class for metric attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    abstract class MetricAttribute : Attribute
    {
        /// <summary>
        /// Defines the metric in a given context.
        /// </summary>
        public abstract object DefineMetric(MetricsContext context);
    }
}