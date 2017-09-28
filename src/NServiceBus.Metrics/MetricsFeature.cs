using System.Linq;
using System.Threading.Tasks;
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

        context.RegisterStartupTask(new SetupRegisteredObservers(options, probeContext));
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