namespace NServiceBus.Metrics
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Features;
    using global::Metrics;

    /// <summary>
    /// Defines a custom metric.
    /// </summary>
    abstract class MetricBuilder
    {
        /// <summary>
        /// Defines the metric facades.
        /// </summary>
        public void Define(MetricsContext metricsContext)
        {
            var fieldsWithAttribute = from field in GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                let metricAttribute = field.GetCustomAttribute<MetricAttribute>()
                where metricAttribute != null
                select new
                {
                    field,
                    metricAttribute
                };

            foreach (var fieldWithAttribute in fieldsWithAttribute)
            {
                var metricFacade = fieldWithAttribute.metricAttribute.DefineMetric(metricsContext);
                if (metricFacade.GetType() != fieldWithAttribute.field.FieldType)
                {
                    throw new InvalidOperationException("A metric attribute must match the type of the metric field: " + fieldWithAttribute.field.Name);
                }
                fieldWithAttribute.field.SetValue(this, metricFacade);
            }
        }

        /// <summary>
        /// Called to Wire up the metric facades with metric updating code.
        /// </summary>
        public abstract void WireUp(FeatureConfigurationContext featureConfigurationContext);
    }
}