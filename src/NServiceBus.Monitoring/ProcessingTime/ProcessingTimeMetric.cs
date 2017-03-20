namespace NServiceBus.Monitoring.ProcessingTime
{
    using System;
    using System.Threading.Tasks;
    using Features;
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
            reporter = new ReportProcessingTime();
        }

        /// <summary>
        /// This enables reporting of the ProcessingTime metric. To disable this feature, call: endpointConfig.DisableFeature&lt;ProcessingTimeMetric&gt;() 
        /// </summary>
        /// <param name="context">feature context</param>
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.ThrowIfSendonly();
            context.Pipeline.OnReceivePipelineCompleted(e =>
            {
                DateTime timeSent;
                if (e.TryGetTimeSent(out timeSent))
                {
                    string messageTypeProcessed;
                    e.TryGetMessageHandlerType(out messageTypeProcessed);
                    reporter.Report(timeSent, messageTypeProcessed, e.StartedAt, e.CompletedAt);
                }
                return Task.FromResult(0);
            });            
        }

        ReportProcessingTime reporter;
    }
}
