using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Hosting;
using NServiceBus.Logging;
using NServiceBus.Metrics;
using NServiceBus.Metrics.ProbeBuilders;
using NServiceBus.Metrics.QueueLength;
using NServiceBus.Metrics.RawData;
using NServiceBus.ObjectBuilder;
using NServiceBus.Support;
using NServiceBus.Transport;
using NServiceBus.Transports;

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

    void SetUpServiceControlReporting(FeatureConfigurationContext context, MetricsOptions metricsOptions, string endpointName, ProbeContext probeContext)
    {
        if (!string.IsNullOrEmpty(metricsOptions.ServiceControlMetricsAddress))
        {
            MetricsContext metricsContext = new DefaultMetricsContext(endpointName);

            SetUpQueueLengthReporting(context, metricsContext);

            SetUpSignalReporting(probeContext, metricsContext);

            context.Container.ConfigureComponent(() => metricsOptions, DependencyLifecycle.SingleInstance);

            Func<IBuilder, Dictionary<string, string>> buildBaseHeaders = b => BuildHeaders(metricsOptions, endpointName, b);

            this.RegisterStartupTask<>();

            context.RegisterStartupTask(builder =>
            {
                var headers = buildBaseHeaders(builder);

                return new ServiceControlReporting(metricsContext, builder, metricsOptions, headers);
            });

            context.RegisterStartupTask(builder =>
            {
                var headers = buildBaseHeaders(builder);

                return new ServiceControlRawDataReporting(probeContext, builder, metricsOptions, headers);
            });
        }
    }

    static Dictionary<string, string> BuildHeaders(MetricsOptions metricsOptions, string endpointName, IBuilder b)
    {
        var hostInformation = b.Build<HostInformation>();

        var headers = new Dictionary<string, string>
        {
            {Headers.OriginatingEndpoint, endpointName},
            {Headers.OriginatingMachine, RuntimeEnvironment.MachineName},
            {Headers.OriginatingHostId, hostInformation.HostId.ToString("N")},
            {Headers.HostDisplayName, hostInformation.DisplayName},
        };

        var instanceId = metricsOptions.EndpointInstanceIdOverride;

        if (string.IsNullOrEmpty(instanceId) == false)
        {
            headers.Add(MetricHeaders.MetricInstanceId, instanceId);
        }

        return headers;
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

            durationProbe.Register(v => timer.Record((long) v.TotalMilliseconds, TimeUnit.Milliseconds));
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
            new MessageProcessingSuccessProbeBuilder(performanceDiagnosticsBehavior),
            new RetriesProbeBuilder(context)
        };

        return new ProbeContext(
            durationBuilders.Select(b => b.Build()).ToArray(),
            signalBuilders.Select(b => b.Build()).ToArray()
        );
    }

    class ServiceControlReporting : FeatureStartupTask
    {
        public ServiceControlReporting(MetricsContext metricsContext, IBuilder builder, MetricsOptions options, Dictionary<string, string> headers)
        {
            this.builder = builder;
            this.options = options;
            this.headers = headers;

            headers.Add(Headers.EnclosedMessageTypes, "NServiceBus.Metrics.MetricReport");
            headers.Add(Headers.ContentType, ContentTypes.Json);

            metricsConfig = new MetricsConfig(metricsContext);
        }

        protected override void OnStart()
        {
            var serviceControlReport = new NServiceBusMetricReport(builder.Build<ISendMessages>(), options, headers);

            metricsConfig.WithReporting(mr => mr.WithReport(serviceControlReport, options.ServiceControlReportingInterval));
        }

        protected override void OnStop()
        {
            metricsConfig.Dispose();
        }

        readonly IBuilder builder;
        readonly MetricsOptions options;
        readonly MetricsConfig metricsConfig;
        readonly Dictionary<string, string> headers;
    }

    class ServiceControlRawDataReporting : FeatureStartupTask
    {
        public ServiceControlRawDataReporting(ProbeContext probeContext, IBuilder builder, MetricsOptions options, Dictionary<string, string> headers)
        {
            this.probeContext = probeContext;
            this.builder = builder;
            this.options = options;
            this.headers = headers;

            reporters = new List<RawDataReporter>();
        }

        protected override void OnStart()
        {
            foreach (var durationProbe in probeContext.Durations)
            {
                if (durationProbe.Name == ProcessingTimeProbeBuilder.ProcessingTime ||
                    durationProbe.Name == CriticalTimeProbeBuilder.CriticalTime)
                {
                    reporters.Add(CreateReporter(durationProbe));
                }
            }

            foreach (var signalProbe in probeContext.Signals)
            {
                if (signalProbe.Name == RetriesProbeBuilder.Retries)
                {
                    reporters.Add(CreateReporter(signalProbe));       
                }
            }

            foreach (var reporter in reporters)
            {
                reporter.Start();
            }
        }

        RawDataReporter CreateReporter(IDurationProbe probe)
        {
            return CreateReporter(
                w => probe.Register(v => w((long)v.TotalMilliseconds)),
                $"{probe.Name.Replace(" ", string.Empty)}",
                "LongValueOccurrence",
                (e, w) => LongValueWriter.Write(w, e));
        }

        RawDataReporter CreateReporter(ISignalProbe probe)
        {
            return CreateReporter(
                w => probe.Register(() => w(1)),
                $"{probe.Name.Replace(" ", string.Empty)}",
                "Occurrence",
                (e, w) => OccurrenceWriter.Write(w, e));
        }

        RawDataReporter CreateReporter(Action<Action<long>> setupProbe, string metricType, string contentType, WriteOutput outputWriter)
        {
            var buffer = new RingBuffer();

            var reporterHeaders = new Dictionary<string, string>(headers);

            reporterHeaders.Add(Headers.ContentType, contentType);
            reporterHeaders.Add(MetricHeaders.MetricType, metricType);

            var reporter = new RawDataReporter(
                builder.Build<ISendMessages>(),
                options.ServiceControlMetricsAddress,
                reporterHeaders,
                buffer,
                outputWriter);

            setupProbe(v =>
            {
                var written = false;
                var attempts = 0;

                while (!written)
                {
                    written = buffer.TryWrite(v);

                    attempts++;

                    if (attempts >= MaxExpectedWriteAttempts)
                    {
                        log.Warn($"Failed to buffer metrics data for ${metricType} after ${attempts} attempts.");
                        attempts = 0;
                    }
                }
            });

            return reporter;
        }

        protected override void OnStop()
        {
            foreach (var reporter in reporters)
            {
                reporter.Stop();
                reporter.Dispose();
            }
        }

        readonly ProbeContext probeContext;
        readonly IBuilder builder;
        readonly MetricsOptions options;
        readonly Dictionary<string, string> headers;
        readonly List<RawDataReporter> reporters;

        const int MaxExpectedWriteAttempts = 10;

        static readonly ILog log = LogManager.GetLogger<ServiceControlRawDataReporting>();
    }
}