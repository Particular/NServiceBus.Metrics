namespace NServiceBus.Metrics
{
    using System.Threading.Tasks;
    using Features;
    using global::Metrics;

    class MetricsFeature : Feature
    {
        /// <summary>
        /// This enables reporting of the ProcessingTime metric. To disable this feature, call: endpointConfig.DisableFeature&lt;ProcessingTimeMetric&gt;()
        /// </summary>
        /// <param name="context">feature context</param>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.ThrowIfSendonly();

            var metricsOptions = context.Settings.Get<MetricsOptions>();

            // TODO: Confirm context name
            MetricsContext metricsContext = new DefaultMetricsContext(context.Settings.EndpointName());

            ConfigureMetrics(context, metricsContext);

            context.RegisterStartupTask(builder => new MetricsReporting(metricsContext, metricsOptions));
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

                processingTimeTimer.Record((long) processingTimeInMilliseconds, TimeUnit.Milliseconds, messageTypeProcessed);

                return Task.FromResult(0);
            });
        }

        class MetricsReporting : FeatureStartupTask
        {
            readonly MetricsOptions metricsOptions;
            readonly MetricsConfig metricsConfig;

            public MetricsReporting(MetricsContext metricsContext, MetricsOptions metricsOptions)
            {
                this.metricsOptions = metricsOptions;
                metricsConfig = new MetricsConfig(metricsContext);
            }

            protected override Task OnStart(IMessageSession session)
            {
                metricsConfig.WithReporting(reports =>
                {
                    if (metricsOptions.EnableReportingToTrace)
                    {
                        var traceReporter = new TraceReport();
                        var traceInterval = metricsOptions.TracingInterval ?? metricsOptions.DefaultInterval;
                        reports.WithReport(traceReporter, traceInterval);
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

    }
}
