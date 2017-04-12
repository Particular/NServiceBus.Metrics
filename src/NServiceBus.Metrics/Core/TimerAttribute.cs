namespace NServiceBus.Metrics
{
    using global::Metrics;

    /// <summary>
    /// Defines a timer.
    /// </summary>
    sealed class TimerAttribute : MetricAttribute
    {
        /// <summary>
        /// Defines a timer.
        /// </summary>
        public TimerAttribute(string name, string unit, string[] tags = null) : base(name, unit, tags)
        {
        }

        /// <summary>
        /// Defines the metric in a given context.
        /// </summary>
        public override object DefineMetric(MetricsContext context)
        {
            return context.Timer(Name, Unit, SamplingType.Default, TimeUnit.Seconds, TimeUnit.Milliseconds, Tags);
        }
    }
}