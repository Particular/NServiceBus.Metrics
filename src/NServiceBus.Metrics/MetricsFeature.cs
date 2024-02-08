namespace NServiceBus.Metrics
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Features;
    using ProbeBuilders;

    /// <summary>
    /// Provides the infrastructure that notifies observers about collected metrics.
    /// </summary>
    public class MetricsFeature : Feature
    {
        internal MetricsFeature() =>
            Defaults(settings =>
            {
                var bindingFlags = BindingFlags.NonPublic | BindingFlags.Instance;
                // Unfortunately the constructor is internal, so we have to use reflection to create an instance of RecoverabilitySettings
                var recoverability = (RecoverabilitySettings)Activator.CreateInstance(typeof(RecoverabilitySettings), bindingFlags, null, [settings], null)!;
                var options = settings.GetOrCreate<MetricsOptions>();
                recoverability.Immediate(c => c.OnMessageBeingRetried((m, ct) => options.Immediate(m, ct)));
                recoverability.Delayed(c => c.OnMessageBeingRetried((m, ct) => options.Delayed(m, ct)));
            });

        /// <inheritdoc />
        protected override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings;
            settings.ThrowIfSendOnly();

            var options = settings.Get<MetricsOptions>();

            var probeContext = BuildProbes(context, options);

            context.RegisterStartupTask(new SetupRegisteredObservers(options, probeContext));
        }

        static ProbeContext BuildProbes(FeatureConfigurationContext context, MetricsOptions options)
        {
            var durationBuilders = new DurationProbeBuilder[]
            {
                new CriticalTimeProbeBuilder(context), new ProcessingTimeProbeBuilder(context)
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
                new RetriesProbeBuilder(options)
            };

            return new ProbeContext(
                durationBuilders.Select(b => b.Build()).ToArray(),
                signalBuilders.Select(b => b.Build()).ToArray()
            );
        }

        class SetupRegisteredObservers(MetricsOptions options, ProbeContext probeContext) : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                options.SetUpObservers(probeContext);
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.FromResult(0);
        }
    }
}