using System;
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
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.ThrowIfSendonly();

        var settings = context.Settings;
        var metricsOptions = settings.Get<MetricsOptions>();


        if (metricsOptions.ProbeObservers.Any() ||
            metricsOptions.ReportDefintions.Any())
        {
            var metrics = new ProbeBuilder[]
            {
                new CriticalTimeProbeBuilder(),
                new PerformanceStatisticsProbeBuilder(),
                new ProcessingTimeProbeBuilder()
            };

            var probes = metrics.SelectMany(pb => pb.WireUp(context)).ToArray();
            
            foreach (var probeObserver in metricsOptions.ProbeObservers)
            {
                probeObserver.Invoke(probes);
            }

            var queueLengthTracker = new QueueLenghtSequenceNumberTracker();
            queueLengthTracker.WireUp(context);

            Func<DefaultMetricsContext> buildContext = () =>
            {
                var metricContext = BuildMetricContext(settings.EndpointName(), probes);
                queueLengthTracker.AttachMetricsContext(metricContext);

                return metricContext;
            };

            context.RegisterStartupTask(builder => new MetricsReporting(buildContext, metricsOptions.ReportDefintions.ToArray(), builder));
        }
    }

    static DefaultMetricsContext BuildMetricContext(string endpointName, Probe[] probes)
    {
        var context = new DefaultMetricsContext($"{endpointName}");

        foreach (var probe in probes)
        {
            if (probe.Type == MeasurementValueType.Count)
            {
                var counter = context.Counter(probe.Name, string.Empty, default(MetricTags));

                probe.MeasurementTaken += v => counter.Increment(v);
            }
            else if (probe.Type == MeasurementValueType.Time)
            {
                var timer = context.Timer(probe.Name, string.Empty, SamplingType.LongTerm, TimeUnit.Seconds, TimeUnit.Milliseconds, default(MetricTags));

                probe.MeasurementTaken += v => timer.Record(v, TimeUnit.Milliseconds);
            }
        }

        return context;
    }

    class MetricsReporting : FeatureStartupTask
    {
        public MetricsReporting(Func<DefaultMetricsContext> buildContext, MetricsOptions.ReportDefintion[] reportDefinitions, IBuilder builder)
        {
            this.buildContext = buildContext;
            this.reportDefinitions = reportDefinitions;
            this.builder = builder;

            stopping = new CancellationTokenSource();
        }

        protected override Task OnStart(IMessageSession session)
        {
            foreach (var reportDefinition in reportDefinitions)
            {
                var context = buildContext();

                var report = reportDefinition.Builder(builder);

                ReportInIntervals(report, reportDefinition.Interval, context).IgnoreContinuation();
            }

            return Task.FromResult(0);
        }

        async Task ReportInIntervals(MetricsReport report, TimeSpan interval, MetricsContext context)
        {
            var reportWindowStart = DateTime.UtcNow;

            while (!stopping.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(interval).ConfigureAwait(false);

                    var reportWindowEnd = DateTime.UtcNow;

                    var metricsData = context.DataProvider.CurrentMetricsData;
                    var dataSnapshot = metricsData.SnapshotMetrics(reportWindowStart, reportWindowEnd);

                    report.RunReport(dataSnapshot, HealthChecks.GetStatus, stopping.Token);

                    reportWindowStart = reportWindowEnd;
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

        readonly Func<DefaultMetricsContext> buildContext;
        readonly MetricsOptions.ReportDefintion[] reportDefinitions;
        readonly IBuilder builder;

        readonly CancellationTokenSource stopping;

        static readonly ILog log = LogManager.GetLogger<MetricsReporting>();
    }
}