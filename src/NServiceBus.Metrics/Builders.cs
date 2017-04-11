static class Builders
{
    public static IMetricBuilder[] Create() => new IMetricBuilder[]
    {
        new PerformanceStatisticsMetricBuilder(),
        new ProcessingTimeMetricBuilder(),
        new CriticalTimeMetricBuilder()
    };
}