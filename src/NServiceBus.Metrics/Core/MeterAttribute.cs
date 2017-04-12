namespace NServiceBus.Metrics
{
    using global::Metrics;

    /// <summary>
    /// Defines a meter.
    /// </summary>
    /// <remarks>
    /// Do not attempt to refactor common properties into the base class. This will break downstreams using cecil to
    /// reflect the attribute data.
    /// </remarks>
    sealed class MeterAttribute : MetricAttribute
    {
        /// <summary>
        /// Defines a meter
        /// </summary>
        public MeterAttribute(string name, string unit, string description, string[] tags = null)
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
        public override object DefineMetric(MetricsContext context)
        {
            return context.Meter(Name, Unit, TimeUnit.Seconds, Tags);
        }
    }
}