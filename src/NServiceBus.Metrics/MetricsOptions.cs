namespace NServiceBus
{
    using System;
    using Metrics;

    /// <summary>
    /// Provides configuration options for Metrics feature
    /// </summary>
    public class MetricsOptions
    {
        /// <summary>
        /// Enables sending periodic updates of metric data to ServiceControl
        /// </summary>
        /// <param name="serviceControlMetricsAddress">The transport address of the ServiceControl instance</param>
        /// <param name="interval">How frequently metric data is sent to ServiceControl</param>
        /// <remarks>If no interval is specified then the Default Interval is used</remarks>
        public void SendMetricDataToServiceControl(string serviceControlMetricsAddress, TimeSpan? interval = null)
        {
            Guard.AgainstNullAndEmpty(nameof(serviceControlMetricsAddress), serviceControlMetricsAddress);
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            ServiceControlAddress = serviceControlMetricsAddress;
            ServiceControlInterval = interval;
        }

        /// <summary>
        /// Enables sending metric data to the trace log
        /// </summary>
        /// <param name="interval">How often metric data is sent to the trace log</param>
        /// <remarks>If no interval is specified then the Default Interval is used</remarks>
        public void EnableMetricTracing(TimeSpan? interval = null)
        {
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            EnableReportingToTrace = true;
            TracingInterval = interval;
        }

        /// <summary>
        /// Sets the default interval for reporting metric data
        /// </summary>
        /// <param name="interval">The new default interval</param>
        public void SetDefaultInterval(TimeSpan interval)
        {
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            DefaultInterval = interval;
        }

        internal string ServiceControlAddress { get; private set; }
        internal TimeSpan? ServiceControlInterval { get; private set; }
        internal bool EnableReportingToTrace { get; private set; }
        internal TimeSpan? TracingInterval { get; private set; }
        internal TimeSpan? DefaultInterval { get; private set; }
    }
}
