namespace NServiceBus.Metrics
{
    using System;

    /// <summary>
    /// Provides configuration options for Metrics feature
    /// </summary>
    public class MetricsOptions
    {
        /// <summary>
        /// Enables sending metric data to the trace log
        /// </summary>
        /// <param name="interval">How often metric data is sent to the trace log</param>
        /// <remarks>If no interval is specified then the Default Interval is used</remarks>
        public void EnableMetricTracing(TimeSpan? interval = null)
        {
            EnableReportingToTrace = true;
            TracingInterval = interval;
        }

        /// <summary>
        /// Sets the default interval for reporting metric data
        /// </summary>
        /// <param name="interval">The new default interval</param>
        public void SetDefaultInterval(TimeSpan interval)
        {
            DefaultInterval = interval;
        }

        internal bool EnableReportingToTrace { get; private set; }
        internal TimeSpan? TracingInterval { get; private set; }
        internal TimeSpan DefaultInterval { get; private set; } = TimeSpan.FromSeconds(30);
    }
}
