using System.Linq;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics.ProbeBuilders;

class MetricsFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.ThrowIfSendonly();

        var probeContext = BuildProbes(context);

        var settings = context.Settings;
        var options = settings.Get<MetricsOptions>();
        var endpointName = settings.EndpointName();

        SetUpRegisteredObservers(context, options, probeContext);

        SetUpLegacyReporters(context, options, endpointName, probeContext);
    }

    static void SetUpRegisteredObservers(FeatureConfigurationContext context, MetricsOptions options, ProbeContext probeContext)
    {
        context.RegisterStartupTask(new SetupRegisteredObservers(options, probeContext));
    }

    void SetUpLegacyReporters(FeatureConfigurationContext featureContext, MetricsOptions options, string endpointName, ProbeContext probeContext)
    {
        var metricsContext = new DefaultMetricsContext(endpointName);
        var metricsConfig = new MetricsConfig(metricsContext);

        SetUpSignalReporting(probeContext, metricsContext);

        SetUpDurationReporting(probeContext, metricsContext);

        options.SetUpLegacyReports(metricsConfig);
    }

    static void SetUpSignalReporting(ProbeContext probeContext, MetricsContext metricsContext)
    {
        foreach (var signalProbe in probeContext.Signals)
        {
            var meter = metricsContext.Meter(signalProbe.Name, string.Empty);

            signalProbe.Register((ref SignalEvent e) => meter.Mark());
        }
    }

    static void SetUpDurationReporting(ProbeContext probeContext, DefaultMetricsContext metricsContext)
    {
        foreach (var durationProbe in probeContext.Durations)
        {
            var timer = metricsContext.Timer(durationProbe.Name, "Messages", SamplingType.Default, TimeUnit.Seconds, TimeUnit.Milliseconds, default(MetricTags));

            durationProbe.Register((ref DurationEvent e) => timer.Record((long)e.Duration.TotalMilliseconds, TimeUnit.Milliseconds));
        }
    }

    static ProbeContext BuildProbes(FeatureConfigurationContext context)
    {
        var durationBuilders = new DurationProbeBuilder[]
        {
            new CriticalTimeProbeBuilder(context),
            new ProcessingTimeProbeBuilder(context)
        };

        var performanceDiagnosticsBehavior = new ReceivePerformanceDiagnosticsBehavior();

        context.Pipeline.Register(
            "NServiceBus.Metrics.ReceivePerformanceDiagnosticsBehavior",
            performanceDiagnosticsBehavior,
            "Provides various performance counters for receive statistics"
        );

        var signalBuilders = new SignalProbeBuilder[]
        {
            new MessagePulledFromQueueProbeBuilder(performanceDiagnosticsBehavior),
            new MessageProcessingFailureProbeBuilder(performanceDiagnosticsBehavior),
            new MessageProcessingSuccessProbeBuilder(performanceDiagnosticsBehavior),
            new RetriesProbeBuilder(context)
        };

        return new ProbeContext(
            durationBuilders.Select(b => b.Build()).ToArray(),
            signalBuilders.Select(b => b.Build()).ToArray()
        );
    }

    class SetupRegisteredObservers : FeatureStartupTask
    {
        readonly MetricsOptions options;
        readonly ProbeContext probeContext;

        public SetupRegisteredObservers(MetricsOptions options, ProbeContext probeContext)
        {
            this.options = options;
            this.probeContext = probeContext;
        }

        protected override Task OnStart(IMessageSession session)
        {
            options.SetUpObservers(probeContext);
            return Task.FromResult(0);
        }

        protected override Task OnStop(IMessageSession session) => Task.FromResult(0);
    }
}