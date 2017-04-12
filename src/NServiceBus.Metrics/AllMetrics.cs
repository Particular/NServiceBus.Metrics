namespace NServiceBus.Metrics
{
    using System;
    using System.IO;
    using System.Linq;
    using global::Metrics;
    using global::Metrics.Core;
    using global::Metrics.Json;
    using global::Metrics.MetricData;
    using global::Metrics.Utils;

    /// <summary>
    /// Allows to define all avaible metrics
    /// </summary>
    public static class AllMetrics
    {
        internal static IMetricBuilder[] Create() => new IMetricBuilder[]
        {
            new PerformanceStatisticsMetricBuilder(),
            new ProcessingTimeMetricBuilder(),
            new CriticalTimeMetricBuilder()
        };

        /// <summary>
        /// Outputs a raw json definition of all the defined metrics into the defined text writer.
        /// </summary>
        /// <param name="writer">The text writer.</param>
        public static void Define(TextWriter writer)
        {
            var context = new StaticMetricsContext();
            using (new MetricsConfig(context))
            {
                var builders = Create();
                foreach (var builder in builders)
                {
                    builder.Define(context);
                }

                var metricsData = context.DataProvider.CurrentMetricsData;
                writer.WriteLine(JsonBuilderV2.BuildJson(metricsData, Enumerable.Empty<EnvironmentEntry>(), new StaticClock(), true));
            }
        }

        class StaticMetricsContext : BaseMetricsContext
        {
            static Clock staticClock = new StaticClock();

            public StaticMetricsContext()
                : this(string.Empty) { }

            public StaticMetricsContext(string context)
                : base(context, new DefaultMetricsRegistry(), new DefaultMetricsBuilder(), () => staticClock.UTCDateTime)
            { }

            protected override MetricsContext CreateChildContextInstance(string contextName)
            {
                return new StaticMetricsContext(contextName);
            }
        }

        class StaticClock : Clock
        {
            public override long Nanoseconds { get; } = 0;
            public override DateTime UTCDateTime { get; } = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified);
        }
    }
}