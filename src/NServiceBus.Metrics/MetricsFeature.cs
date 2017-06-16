using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Metrics;
using Metrics.Reporters;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Logging;
using NServiceBus.Metrics;
using NServiceBus.Metrics.QueueLength;
using NServiceBus.ObjectBuilder;

class MetricsFeature : Feature
{
    public MetricsFeature()
    {
        Defaults(s => { s.Set<MetricsRegistry>(new MetricsRegistry()); });
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        context.ThrowIfSendonly();

        var resetMetricTimer = new ResetMetricTimer(context);

        context.RegisterMetricBuilder(new CriticalTimeMetricBuilder(resetMetricTimer));
        context.RegisterMetricBuilder(new PerformanceStatisticsMetricBuilder());
        context.RegisterMetricBuilder(new ProcessingTimeMetricBuilder(resetMetricTimer));
        context.RegisterMetricBuilder(new QueueLengthMetricBuilder());

        var settings = context.Settings;
        var metricsOptions = settings.Get<MetricsOptions>();

        // the context is used as originating endpoint in the headers
        MetricsContext metricsContext = new DefaultMetricsContext($"{settings.EndpointName()}");

        ConfigureMetrics(context, metricsContext);

        context.RegisterStartupTask(builder => new MetricsReporting(metricsOptions.ReportBuilders, metricsContext, builder, metricsOptions.ReportingInterval));
    }


    static void ConfigureMetrics(FeatureConfigurationContext context, MetricsContext metricsContext)
    {
        var builders = context.Settings.Get<MetricsRegistry>().Builders;

        foreach (var builder in builders)
        {
            builder.Define(metricsContext);
            builder.WireUp(context);
        }
    }

    class MetricsReporting : FeatureStartupTask
    {
        public MetricsReporting(List<Func<IBuilder, MetricsReport>> reportBulders, MetricsContext metricContext, IBuilder builder, TimeSpan reportingInterval)
        {
            this.reportBulders = reportBulders;
            this.metricContext = metricContext;
            this.builder = builder;
            this.reportingInterval = reportingInterval;

            stopping = new CancellationTokenSource();
        }

        protected override Task OnStart(IMessageSession session)
        {
            var reports = reportBulders.Select(rb => rb(builder)).ToArray();

            ReportInIntervals(reports).IgnoreContinuation();

            return Task.FromResult(0);
        }

        async Task ReportInIntervals(MetricsReport[] reports)
        {
            var reportWindowStart = DateTime.UtcNow;

            while (!stopping.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(reportingInterval).ConfigureAwait(false);

                    var metricsData = metricContext.DataProvider.CurrentMetricsData;

                    var now = DateTime.UtcNow;
                    var dataSnapshot = metricsData.SnapshotMetrics(reportWindowStart, now);

                    reportWindowStart = now;

                    foreach (var report in reports)
                    {
                        try
                        {
                            report.RunReport(dataSnapshot, HealthChecks.GetStatus, stopping.Token);
                        }
                        catch (Exception ex)
                        {
                            log.Error($"Error generating report {report.GetType().FullName}.", ex);
                        }

                    }
                }
                catch (Exception e)
                {
                    //HINT: There is not much we can do except for logging
                    log.Error("Error while reporting metrics information.", e);
                }
            }
        }

        protected override Task OnStop(IMessageSession session)
        {
            stopping.Cancel();

            return Task.FromResult(0);
        }

        List<Func<IBuilder, MetricsReport>> reportBulders;
        MetricsContext metricContext;
        IBuilder builder;
        TimeSpan reportingInterval;

        CancellationTokenSource stopping;

        static ILog log = LogManager.GetLogger<MetricsReporting>();
    }
}