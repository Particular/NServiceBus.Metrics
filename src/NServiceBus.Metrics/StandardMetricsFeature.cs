using NServiceBus.Features;
using NServiceBus.Metrics;

class StandardMetricsFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.RegisterMetricBuilder(new CriticalTimeMetricBuilder());
        context.RegisterMetricBuilder(new PerformanceStatisticsMetricBuilder());
        context.RegisterMetricBuilder(new ProcessingTimeMetricBuilder());
    }
}