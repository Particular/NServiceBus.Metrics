using Metrics;
using NServiceBus.Features;

interface IMetricBuilder
{
    void Define(MetricsContext metricsContext);

    void WireUp(FeatureConfigurationContext featureConfigurationContext);
}