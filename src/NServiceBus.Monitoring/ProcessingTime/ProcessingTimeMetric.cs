namespace NServiceBus.Monitoring.ProcessingTime
{
    using System;
    using System.Threading.Tasks;
    using Features;
    using Metrics;
    using Metrics.MetricData;

    /// <summary>
    /// Hooks into the NServiceBus pipeline and calculates Processing Time.
    /// This metric will be periodically sent to the Metrics Processing Component via a configured NServiceBus transport.
    /// </summary>
    public class ProcessingTimeMetric : Feature
    {
        /// <summary>
        /// Enables the ProcessingTimeMetric feature
        /// </summary>
        /// <returns></returns>
        public ProcessingTimeMetric()
        {
            EnableByDefault();
        }

        /// <summary>
        /// This enables reporting of the ProcessingTime metric. To disable this feature, call: endpointConfig.DisableFeature&lt;ProcessingTimeMetric&gt;()
        /// </summary>
        /// <param name="context">feature context</param>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.ThrowIfSendonly();

            // TODO: Confirm context name
            MetricsContext metricsContext = new DefaultMetricsContext(context.Settings.EndpointName());
            // TODO: Configure a proper Metrics.NET reporter
            var metricsConfig = new MetricsConfig(metricsContext);
            metricsConfig.WithReporting(reports => reports.WithReport(new TraceReport(), TimeSpan.FromSeconds(5)));

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
    }
}
