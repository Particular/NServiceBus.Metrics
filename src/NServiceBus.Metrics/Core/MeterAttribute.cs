namespace NServiceBus.Metrics
{
    using global::Metrics;

    /// <summary>
    /// Defines a meter.
    /// </summary>
    sealed class MeterAttribute : MetricAttribute
    {
        /// <summary>
        /// Defines a meter
        /// </summary>
        public MeterAttribute(string name, string unit, string[] tags = null) : base(name, unit, tags)
        {
        }

        /// <summary>
        /// Defines the metric in a given context.
        /// </summary>
        public override object DefineMetric(MetricsContext context)
        {
            return context.Meter(Name, Unit, TimeUnit.Seconds, Tags);
        }
    }
}