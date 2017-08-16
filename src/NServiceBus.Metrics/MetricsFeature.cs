﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Hosting;
using NServiceBus.Logging;
using NServiceBus.MessageMutator;
using NServiceBus.Metrics;
using NServiceBus.Metrics.ProbeBuilders;
using NServiceBus.Metrics.QueueLength;
using NServiceBus.Metrics.RawData;
using NServiceBus.ObjectBuilder;
using NServiceBus.Support;
using NServiceBus.Transport;
using TaskExtensions = NServiceBus.Metrics.TaskExtensions;

class MetricsFeature : Feature
{
    static readonly IReadOnlyDictionary<string, string> MetricsDotNetMapName = new Dictionary<string, string>
    {
        {Probes.RetryOccurred, "Retries"},
        {Probes.CriticalTime, "Critical Time"},
        {Probes.MessageFailed, "# of msgs failures / sec"},
        {Probes.MessageProcessed, "# of msgs successfully processed / sec"},
        {Probes.MessagePulled, "# of msgs pulled from the input queue /sec"},
        {Probes.ProcessingTime, "Processing Time"}
    };

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

        SetUpOutgoingMessageMutator(context, options);
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

                string instanceId;
                if (metricsOptions.TryGetValidEndpointInstanceIdOverride(out instanceId))
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
        string instanceId;
        if (options.TryGetValidEndpointInstanceIdOverride(out instanceId))
        {
            context.Container.ConfigureComponent(() => new MetricsIdAttachingMutator(instanceId), DependencyLifecycle.SingleInstance);
        }
    }

    static void SetUpSignalReporting(ProbeContext probeContext, MetricsContext metricsContext)
    {
        foreach (var signalProbe in probeContext.Signals)
        {
            string name;
            if (MetricsDotNetMapName.TryGetValue(signalProbe.Id, out name))
            {
                var meter = metricsContext.Meter(name, string.Empty);
                signalProbe.Register(() => meter.Mark());
            }
        }
    }

    static void SetUpDurationReporting(ProbeContext probeContext, DefaultMetricsContext metricsContext)
    {
        foreach (var durationProbe in probeContext.Durations)
        {
            string name;
            if (MetricsDotNetMapName.TryGetValue(durationProbe.Id, out name))
            {
                var timer = metricsContext.Timer(name, "Messages", SamplingType.Default, TimeUnit.Seconds, TimeUnit.Milliseconds, default(MetricTags));
                durationProbe.Register(v => timer.Record((long)v.TotalMilliseconds, TimeUnit.Milliseconds));
            }
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
                if (durationProbe.Id == Probes.ProcessingTime ||
                    durationProbe.Id == Probes.CriticalTime)
                {
                    reporters.Add(CreateReporter(durationProbe));
                }
            }

            foreach (var signalProbe in probeContext.Signals)
            {
                if (signalProbe.Id == Probes.RetryOccurred)
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
            return CreateReporter(
                w => probe.Register(v => w((long)v.TotalMilliseconds)),
                probe.Id,
                "LongValueOccurrence",
                (e, w) => LongValueWriter.Write(w, e));
        }

        RawDataReporter CreateReporter(ISignalProbe probe)
        {
            return CreateReporter(
                w => probe.Register(() => w(1)),
                probe.Id,
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
                builder.Build<IDispatchMessages>(),
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

        static readonly int MaxExpectedWriteAttempts = 10;

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