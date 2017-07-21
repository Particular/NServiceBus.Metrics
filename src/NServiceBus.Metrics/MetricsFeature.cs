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

            Func<IBuilder, Dictionary<string, string>> buildBaseHeaders = b =>
            {
                var hostInformation = b.Build<HostInformation>();

                var headers =  new Dictionary<string, string>
                {
                    {Headers.OriginatingEndpoint, endpointName},
                    {Headers.OriginatingMachine, RuntimeEnvironment.MachineName},
                    {Headers.OriginatingHostId, hostInformation.HostId.ToString("N")},
                    {Headers.HostDisplayName, hostInformation.DisplayName },
                };

                var instanceId = metricsOptions.EndpointInstanceIdOverride;

                if (string.IsNullOrEmpty(instanceId) == false)
                {
                    headers.Add(MetricHeaders.MetricInstanceId, instanceId);
                }

                return headers;
            };

            context.RegisterStartupTask(builder =>
            {
                var headers = buildBaseHeaders(builder);

                return new ServiceControlReporting(metricsContext, builder, metricsOptions, headers);
            });

            context.RegisterStartupTask(builder =>
            {
                var headers = buildBaseHeaders(builder);

                return new ServiceControlRawDataReporting(probeContext.Durations, builder, metricsOptions, headers);
            });
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

        protected override Task OnStart(IMessageSession session)
        {
            var serviceControlReport = new NServiceBusMetricReport(builder.Build<IDispatchMessages>(), options, headers);

            metricsConfig.WithReporting(mr => mr.WithReport(serviceControlReport, options.ServiceControlReportingInterval));

            return Task.FromResult(0);
        }

        protected override Task OnStop(IMessageSession session)
        {
            metricsConfig.Dispose();
            return Task.FromResult(0);
        }

        readonly IBuilder builder;
        readonly MetricsOptions options;
        readonly MetricsConfig metricsConfig;
        readonly Dictionary<string, string> headers;
    }

    class ServiceControlRawDataReporting : FeatureStartupTask
    {
        public ServiceControlRawDataReporting(IReadOnlyCollection<IDurationProbe> probes, IBuilder builder, MetricsOptions options, Dictionary<string, string> headers)
        {
            this.probes = probes;
            this.builder = builder;
            this.options = options;
            this.headers = headers;
        }

        protected override Task OnStart(IMessageSession session)
        {
            foreach (var durationProbe in probes)
            {
                if (durationProbe.Name == ProcessingTimeProbeBuilder.ProcessingTime)
                    processingTimeReporter = CreateRawDataReporter(durationProbe);

                if (durationProbe.Name == CriticalTimeProbeBuilder.CriticalTime)
                    criticalTimeReporter = CreateRawDataReporter(durationProbe);
            }

            processingTimeReporter.Start();
            criticalTimeReporter.Start();

            return Task.FromResult(0);
        }

        RawDataReporter CreateRawDataReporter(IDurationProbe probe)
        {
            var buffer = new RingBuffer();

            var reporterHeaders = new Dictionary<string, string>(headers);

            reporterHeaders.Add(Headers.ContentType, "LongValueOccurrence");
            reporterHeaders.Add(MetricHeaders.MetricType, $"{probe.Name.Replace(" ", string.Empty)}");

            var reporter = new RawDataReporter(
                builder.Build<IDispatchMessages>(),
                options.ServiceControlMetricsAddress,
                reporterHeaders,
                buffer,
                (entries, writer) => LongValueWriter.Write(writer, entries));

            probe.Register(ct =>
            {
                var written = false;
                var attempts = 0;

                while (!written)
                {
                    written = buffer.TryWrite((long) ct.TotalMilliseconds);

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

        readonly IReadOnlyCollection<IDurationProbe> probes;
        readonly IBuilder builder;
        readonly MetricsOptions options;
        readonly Dictionary<string, string> headers;

        RawDataReporter processingTimeReporter;
        RawDataReporter criticalTimeReporter;

        static readonly int MaxExpectedWriteAttempts = 10;

        static readonly ILog log = LogManager.GetLogger<ServiceControlRawDataReporting>();
    }
}