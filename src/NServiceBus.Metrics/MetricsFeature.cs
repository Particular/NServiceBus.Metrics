using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Metrics.ProbeBuilders;

class MetricsFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.ThrowIfSendonly();

        var sensorFactory = new MetricsSensorFactory();

        AddStandardProbes(context, sensorFactory);

        var settings = context.Settings;
        var options = settings.Get<MetricsOptions>();

        context.RegisterStartupTask(new SetupRegisteredObservers(options, sensorFactory));
    }

    static void AddStandardProbes(FeatureConfigurationContext context, MetricsSensorFactory sensorFactory)
    {
        var durationBuilders = new DurationProbeBuilder[]
        {
            new CriticalTimeProbeBuilder(context),
            new ProcessingTimeProbeBuilder(context)
        };

        foreach (var durationProbeBuilder in durationBuilders)
        {
            sensorFactory.AddExisting(durationProbeBuilder.Build());
        }

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

        foreach (var signalProbeBuilder in signalBuilders)
        {
            sensorFactory.AddExisting(signalProbeBuilder.Build());
        }
    }

    class SetupRegisteredObservers : FeatureStartupTask
    {
        readonly MetricsOptions options;
        readonly MetricsSensorFactory sensorFactory;

        public SetupRegisteredObservers(MetricsOptions options, MetricsSensorFactory sensorFactory)
        {
            this.options = options;
            this.sensorFactory = sensorFactory;
        }

        protected override Task OnStart(IMessageSession session)
        {
            var probeContext = sensorFactory.CreateProbeContext();
            options.SetUpObservers(probeContext);
            return Task.FromResult(0);
        }

        protected override Task OnStop(IMessageSession session) => Task.FromResult(0);
    }
}