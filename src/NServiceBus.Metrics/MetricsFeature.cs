using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Hosting;
using NServiceBus.Logging;
using NServiceBus.Metrics.QueueLength;
using NServiceBus.Metrics.RawData;
using NServiceBus.ObjectBuilder;
using NServiceBus.Support;
using NServiceBus.Transport;

class MetricsFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.ThrowIfSendonly();
        
        var probeContext = BuildProbes(context);

        var settings = context.Settings;
        var options = settings.Get<MetricsOptions>();
        var endpointName = settings.EndpointName();

        options.SetUpObservers(probeContext);

        SetUpServiceControlReporting(context, options, endpointName, probeContext);

        SetUpLegacyReporters(context, options, endpointName, probeContext);
    }

    void SetUpLegacyReporters(FeatureConfigurationContext featureContext, MetricsOptions options, string endpointName, ProbeContext probeContext)
    {
        var metricsContext = new DefaultMetricsContext(endpointName);
        var metricsConfig = new MetricsConfig(metricsContext);

        SetUpSignalReporting(probeContext, metricsContext);

        SetUpDurationReporting(probeContext, metricsContext);

        options.SetUpLegacyReports(metricsConfig);
    }

    static void SetUpServiceControlReporting(FeatureConfigurationContext context, MetricsOptions metricsOptions, string endpointName, ProbeContext probeContext)
    {
        if (!string.IsNullOrEmpty(metricsOptions.ServiceControlMetricsAddress))
        {
            MetricsContext metricsContext = new DefaultMetricsContext(endpointName);

            SetUpQueueLengthReporting(context, metricsContext);

            SetUpSignalReporting(probeContext, metricsContext);

            context.RegisterStartupTask(builder => new ServiceControlReporting(metricsContext, builder, metricsOptions));
            context.RegisterStartupTask(builder => new ServiceControlRawDataReporting(probeContext.Durations, builder, metricsOptions, endpointName));
        }
    }

    static void SetUpSignalReporting(ProbeContext probeContext, MetricsContext metricsContext)
    {
        foreach (var signalProbe in probeContext.Signals)
        {
            var meter = metricsContext.Meter(signalProbe.Name, string.Empty);

            signalProbe.Register(() => meter.Mark());
        }
    }

    static void SetUpDurationReporting(ProbeContext probeContext, DefaultMetricsContext metricsContext)
    {
        foreach (var durationProbe in probeContext.Durations)
        {
            var timer = metricsContext.Timer(durationProbe.Name, "Messages", SamplingType.Default, TimeUnit.Seconds, TimeUnit.Milliseconds, default(MetricTags));

            durationProbe.Register(v => timer.Record((long)v.TotalMilliseconds, TimeUnit.Milliseconds));
        }
    }

    static void SetUpQueueLengthReporting(FeatureConfigurationContext context, MetricsContext metricsContext)
    {
        QueueLengthTracker.SetUp(metricsContext, context);
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
            new MessageProcessingSuccessProbeBuilder(performanceDiagnosticsBehavior)
        };

        return new ProbeContext(
            durationBuilders.Select(b => b.Build()).ToArray(),
            signalBuilders.Select(b => b.Build()).ToArray()
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

            metricsConfig.WithReporting(mr => mr.WithReport(serviceControlReport, options.ServiceControlReportingInterval));

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

    class ServiceControlRawDataReporting : FeatureStartupTask
    {
        IReadOnlyCollection<IDurationProbe> probes;
        IBuilder builder;
        MetricsOptions options;
        string endpointName;
        RawDataReporter processingTimeReporter;
        RawDataReporter criticalTimeReporter;

        public ServiceControlRawDataReporting(IReadOnlyCollection<IDurationProbe> probes, IBuilder builder, MetricsOptions options, string endpointName)
        {
            this.probes = probes;
            this.builder = builder;
            this.options = options;
            this.endpointName = endpointName;
        }

        protected override Task OnStart(IMessageSession session)
        {
            foreach (var durationProbe in probes)
            {
                if (durationProbe.Name == ProcessingTimeProbeBuilder.ProcessingTime)
                {
                    processingTimeReporter = CreateRawDataReporter(durationProbe);
                }

                if (durationProbe.Name == CriticalTimeProbeBuilder.CriticalTime)
                {
                    criticalTimeReporter = CreateRawDataReporter(durationProbe);
                }
            }

            processingTimeReporter.Start();
            criticalTimeReporter.Start();

            return Task.FromResult(0);
        }

        RawDataReporter CreateRawDataReporter(IDurationProbe probe)
        {
            var buffer = new RingBuffer();

            var headers = new Dictionary<string, string>
            {
                { Headers.OriginatingMachine, RuntimeEnvironment.MachineName},
                { Headers.OriginatingHostId, builder.Build<HostInformation>().HostId.ToString("N")},
                { Headers.EnclosedMessageTypes, $"NServiceBus.Metrics.{probe.Name.Replace(" ", string.Empty)}"},
                { Headers.OriginatingEndpoint, endpointName },
                { Headers.ContentType, "LongValueOccurrence"}
            };

            var reporter = new RawDataReporter(
                builder.Build<IDispatchMessages>(),
                options.ServiceControlMetricsAddress,
                headers,
                buffer,
                (entries, writer) => LongValueWriter.Write(writer, entries));

            probe.Register(ct =>
            {
                var written = false;
                var attempts = 0;

                while (!written)
                {
                    written = buffer.TryWrite((long)ct.TotalMilliseconds);

                    attempts++;

                    if (attempts >= MaxExpectedWriteAttempts)
                    {
                        log.Warn($"Failed to buffer timing metrics data after ${attempts} attempts.");
                        attempts = 0;
                    }
                }
            });

            return reporter;
        }

        protected override async Task OnStop(IMessageSession session)
        {
            await Task.WhenAll(criticalTimeReporter.Stop(), processingTimeReporter.Stop()).ConfigureAwait(false);

            criticalTimeReporter.Dispose();
            processingTimeReporter.Dispose();
        }

        static int MaxExpectedWriteAttempts = 10;
        static ILog log = LogManager.GetLogger<ServiceControlRawDataReporting>();
    }
}