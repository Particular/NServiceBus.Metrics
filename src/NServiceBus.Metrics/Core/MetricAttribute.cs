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
        /// Creates new metric attribute.
        /// </summary>
        protected MetricAttribute(string name, string unit, string description, string[] tags = null)
        {
            Name = name;
            Unit = unit;
            Description = description;
            Tags = tags;
        }

        /// <summary>
        /// Name of the meter
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Unit of measure
        /// </summary>
        public string Unit { get; }

        /// <summary>
        /// Description.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Optional tags
        /// </summary>
        public string[] Tags { get; }

        /// <summary>
        /// Defines the metric in a given context.
        /// </summary>
        public abstract object DefineMetric(MetricsContext context);
    }
}