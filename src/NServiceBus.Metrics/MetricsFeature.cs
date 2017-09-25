using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NServiceBus.Hosting;
using NServiceBus.Logging;
using NServiceBus.MessageMutator;
using NServiceBus.Metrics;
using NServiceBus.Metrics.ProbeBuilders;
using NServiceBus.Metrics.QueueLength;
using NServiceBus.ObjectBuilder;
using NServiceBus.Routing;
using NServiceBus.Support;
using NServiceBus.Transport;
using ServiceControl.Monitoring.Data;
using TaskExtensions = NServiceBus.Metrics.TaskExtensions;

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

        SetUpServiceControlReporting(context, options, endpointName, probeContext);

        SetUpLegacyReporters(context, options, endpointName, probeContext);

        SetUpOutgoingMessageMutator(context, options);
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

                var headers = new Dictionary<string, string>
                {
                    {Headers.OriginatingEndpoint, endpointName},
                    {Headers.OriginatingMachine, RuntimeEnvironment.MachineName},
                    {Headers.OriginatingHostId, hostInformation.HostId.ToString("N")},
                    {Headers.HostDisplayName, hostInformation.DisplayName },
                };

                if (metricsOptions.TryGetValidEndpointInstanceIdOverride(out var instanceId))
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

                return new ServiceControlRawDataReporting(probeContext, builder, metricsOptions, headers);
            });
        }
    }

    void SetUpOutgoingMessageMutator(FeatureConfigurationContext context, MetricsOptions options)
    {
        if (options.TryGetValidEndpointInstanceIdOverride(out var instanceId))
        {
            context.Container.ConfigureComponent(() => new MetricsIdAttachingMutator(instanceId), DependencyLifecycle.SingleInstance);
        }
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
        const string TaggedValueMetricContentType = "TaggedLongValueWriterOccurrence";

        public ServiceControlRawDataReporting(ProbeContext probeContext, IBuilder builder, MetricsOptions options, Dictionary<string, string> headers)
        {
            this.probeContext = probeContext;
            this.builder = builder;
            this.options = options;
            this.headers = headers;

            reporters = new List<RawDataReporter>();
        }

        protected override Task OnStart(IMessageSession session)
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

            return Task.FromResult(0);
        }

        RawDataReporter CreateReporter(IDurationProbe probe)
        {
            var metricType = GetMetricType(probe);
            var writer = new TaggedLongValueWriterV1();

            return CreateReporter(
                writeAction => probe.Register((ref DurationEvent d) =>
                {
                    var tag = writer.GetTagId(d.MessageType ?? "");
                    writeAction((long)d.Duration.TotalMilliseconds, tag);
                }),
                metricType,
                TaggedValueMetricContentType,
                (entries, binaryWriter) => writer.Write(binaryWriter, entries));
        }

        RawDataReporter CreateReporter(ISignalProbe probe)
        {
            var metricType = GetMetricType(probe);
            var writer = new TaggedLongValueWriterV1();

            return CreateReporter(
                writeAction => probe.Register((ref SignalEvent e) =>
                {
                    var tag = writer.GetTagId(e.MessageType ?? "");
                    writeAction(1, tag);
                }),
                metricType,
                TaggedValueMetricContentType,
                (entries, binaryWriter) => writer.Write(binaryWriter, entries));
        }

        static string GetMetricType(IProbe probe) => $"{probe.Name.Replace(" ", string.Empty)}";

        RawDataReporter CreateReporter(Action<Action<long, int>> setupProbe, string metricType, string contentType, WriteOutput outputWriter)
        {
            var buffer = new RingBuffer();

            var reporterHeaders = new Dictionary<string, string>(headers)
            {
                {Headers.ContentType, contentType},
                {MetricHeaders.MetricType, metricType}
            };

            var dispatcher = builder.Build<IDispatchMessages>();
            var address = new UnicastAddressTag(options.ServiceControlMetricsAddress);

            Func<byte[], Task> sender = async body =>
            {
                var message = new OutgoingMessage(Guid.NewGuid().ToString(), reporterHeaders, body);

                var operation = new TransportOperation(message, address);
                try
                {
                    await dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag())
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Error while reporting raw data to {options.ServiceControlMetricsAddress}.", ex);
                }
            };

            var reporter = new RawDataReporter(sender, buffer, outputWriter);

            setupProbe((value, tag) =>
            {
                var written = false;
                var attempts = 0;

                while (!written)
                {
                    written = buffer.TryWrite(value, tag);

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

        protected override async Task OnStop(IMessageSession session)
        {
            await Task.WhenAll(reporters.Select(r => r.Stop())).ConfigureAwait(false);

            foreach (var reporter in reporters)
            {
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

    class MetricsIdAttachingMutator : IMutateOutgoingMessages
    {
        readonly string instanceId;

        public MetricsIdAttachingMutator(string instanceId)
        {
            this.instanceId = instanceId;
        }

        public Task MutateOutgoing(MutateOutgoingMessageContext context)
        {
            context.OutgoingHeaders[MetricHeaders.MetricInstanceId] = instanceId;
            return TaskExtensions.Completed;
        }
    }
}