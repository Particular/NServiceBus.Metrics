using System.Linq;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Hosting;
using NServiceBus.Metrics;
using NServiceBus.Metrics.QueueLength;
using NServiceBus.ObjectBuilder;
using NServiceBus.Transport;

class MetricsFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.ThrowIfSendonly();
        
        var probeContext = BuildProbes(context);

        var settings = context.Settings;
        var metricsOptions = settings.Get<MetricsOptions>();

        metricsOptions.SetUpObservers(probeContext);

        // the context is used as originating endpoint in the headers
        MetricsContext metricsContext = new DefaultMetricsContext($"{settings.EndpointName()}");

        SetUpQueueLengthReporting(context, metricsContext);

        SetUpSignalReporting(probeContext, metricsContext);

        context.RegisterStartupTask(builder => new ServiceControlReporting(metricsContext, builder, metricsOptions));
    }

    static void SetUpSignalReporting(ProbeContext probeContext, MetricsContext metricsContext)
    {
        probeContext.Signals.ToList().ForEach(sp =>
        {
            var meter = metricsContext.Meter(sp.Name, string.Empty);

            sp.Register(() => meter.Mark());
        });
    }

    static void SetUpQueueLengthReporting(FeatureConfigurationContext context, MetricsContext metricsContext)
    {
        new QueueLengthTracker().SetUp(metricsContext, context);
    }

    static ProbeContext BuildProbes(FeatureConfigurationContext context)
    {
        var durationBuilders = new DurationProbeBuilder[]
        {
            new CriticalTimeProbeBuilder(),
            new ProcessingTimeProbeBuilder()
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
            new MessageProcessingSuccessProbeBuilder(performanceDiagnosticsBehavior)
        };

        return new ProbeContext(
            durationBuilders.Select(b => b.Build(context)).ToArray(),
            signalBuilders.Select(b => b.Build(context)).ToArray()
        );
    }

    class ServiceControlReporting : FeatureStartupTask
    {
        public ServiceControlReporting(MetricsContext metricsContext, IBuilder builder, MetricsOptions options)
        {
            this.builder = builder;
            this.options = options;

            metricsConfig = new MetricsConfig(metricsContext);
        }

        protected override Task OnStart(IMessageSession session)
        {
            var serviceControlReport = new NServiceBusMetricReport(
                builder.Build<IDispatchMessages>(), 
                options.ServiceControlMetricsAddress, 
                builder.Build<HostInformation>());

            metricsConfig.WithReporting(mr => mr.WithReport(serviceControlReport, options.ReportingInterval));

            return Task.FromResult(0);
        }

        protected override Task OnStop(IMessageSession session)
        {
            metricsConfig.Dispose();
            return Task.FromResult(0);
        }

        IBuilder builder;
        MetricsOptions options;
        MetricsConfig metricsConfig;
    }
}