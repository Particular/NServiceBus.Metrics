namespace NServiceBus.Metrics
{
    using System.Collections.Generic;

    class MetricsRegistry
    {
        public IEnumerable<MetricBuilder> Builders => builders;

        /// <summary>
        /// Registers a metric builder.
        /// </summary>
        public void RegisterMetricBuilder(MetricBuilder builder)
        {
            builders.Add(builder);
        }

        List<MetricBuilder> builders = new List<MetricBuilder>();
    }
}