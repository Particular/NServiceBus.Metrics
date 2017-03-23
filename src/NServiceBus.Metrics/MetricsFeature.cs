﻿using System;
using System.Threading.Tasks;
using Metrics;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.ObjectBuilder;
using NServiceBus.Transport;

class MetricsFeature : Feature
{
    protected override void Setup(FeatureConfigurationContext context)
    {
        context.ThrowIfSendonly();

        var metricsOptions = context.Settings.Get<MetricsOptions>();

        // TODO: Confirm context name
        MetricsContext metricsContext = new DefaultMetricsContext(context.Settings.EndpointName());

        ConfigureMetrics(context, metricsContext);

        context.RegisterStartupTask(builder => new MetricsReporting(metricsContext, metricsOptions, builder));
    }

    static void ConfigureMetrics(FeatureConfigurationContext context, MetricsContext metricsContext)
    {
        var messagesUnit = Unit.Custom("Messages");

        var processingTimeTimer = metricsContext.Timer("Processing Time", messagesUnit);

        context.Pipeline.OnReceivePipelineCompleted(e =>
        {
            var processingTimeInMilliseconds = ProcessingTimeCalculator.Calculate(e.StartedAt, e.CompletedAt).TotalMilliseconds;

            string messageTypeProcessed;
            e.TryGetMessageType(out messageTypeProcessed);

            processingTimeTimer.Record((long)processingTimeInMilliseconds, TimeUnit.Milliseconds, messageTypeProcessed);

            return Task.FromResult(0);
        });
    }

    class MetricsReporting : FeatureStartupTask
    {
        MetricsOptions metricsOptions;
        IBuilder builder;
        MetricsConfig metricsConfig;

        public MetricsReporting(MetricsContext metricsContext, MetricsOptions metricsOptions, IBuilder builder)
        {
            this.metricsOptions = metricsOptions;
            this.builder = builder;
            metricsConfig = new MetricsConfig(metricsContext);
        }

        protected override Task OnStart(IMessageSession session)
        {
            metricsConfig.WithReporting(reports =>
            {
                if (metricsOptions.EnableReportingToTrace)
                {
                    var traceReporter = new TraceReport();
                    var traceInterval = metricsOptions.TracingInterval ?? metricsOptions.DefaultInterval ?? DefaultInterval;
                    reports.WithReport(traceReporter, traceInterval);
                }

                if (!string.IsNullOrWhiteSpace(metricsOptions.ServiceControlAddress))
                {
                    var messageDispatcher = builder.Build<IDispatchMessages>();
                    var serviceControlReporter = new NServiceBusMetricReport(messageDispatcher, metricsOptions.ServiceControlAddress);
                    var serviceControlInterval = metricsOptions.ServiceControlInterval ?? metricsOptions.DefaultInterval ?? DefaultInterval;
                    reports.WithReport(serviceControlReporter, serviceControlInterval);
                }
            });
            return Task.FromResult(0);
        }

        protected override Task OnStop(IMessageSession session)
        {
            metricsConfig.Dispose();
            return Task.FromResult(0);
        }
    }

    public static TimeSpan DefaultInterval = TimeSpan.FromSeconds(30);
}