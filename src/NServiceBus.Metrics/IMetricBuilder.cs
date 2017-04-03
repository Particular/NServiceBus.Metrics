using Metrics;
using NServiceBus.Features;

interface IMetricBuilder
{
    void WireUp(FeatureConfigurationContext featureConfigurationContext, MetricsContext metricsContext, Unit messagesUnit);
}