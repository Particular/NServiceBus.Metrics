namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using global::Metrics;
    using global::Metrics.Reports;
    using ObjectBuilder;
    using Transport;

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

            reportInstallers.Add((builder, config) => config.WithReport(
                new NServiceBusMetricReport(builder.Build<IDispatchMessages>(), serviceControlMetricsAddress),
                interval ?? defaultInterval
            ));
        }

        /// <summary>
        /// Enables sending metric data to the trace log
        /// </summary>
        /// <param name="interval">How often metric data is sent to the trace log</param>
        /// <remarks>If no interval is specified then the Default Interval is used</remarks>
        public void EnableMetricTracing(TimeSpan? interval = null)
        {
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            reportInstallers.Add((builder, config) => config.WithReport(
                new TraceReport(),
                interval ?? defaultInterval
            ));
        }

        /// <summary>
        /// Enables sending metric data to the NServiceBus log
        /// </summary>
        /// <param name="interval">How often metric data is sent to the log</param>
        /// <remarks>
        /// If no interval is specified then the Default Interval is used.
        /// Metrics data will be logged at the INFO log level
        /// </remarks>
        public void EnableLogTracing(TimeSpan? interval = null)
        {
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            reportInstallers.Add((builder, config) => config.WithReport(
                new MetricsLogReport(),
                interval ?? defaultInterval
            ));
        }

        /// <summary>
        /// Sets the default interval for reporting metric data
        /// </summary>
        /// <param name="interval">The new default interval</param>
        public void SetDefaultInterval(TimeSpan interval)
        {
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            defaultInterval = interval;
        }

        internal void SetUpReports(MetricsConfig config, IBuilder builder)
        {
            config.WithReporting(reportsConfig => reportInstallers.ForEach(installer => installer(builder, reportsConfig)));
        }

        TimeSpan defaultInterval = TimeSpan.FromSeconds(30);
        List<Action<IBuilder, MetricsReports>> reportInstallers = new List<Action<IBuilder, MetricsReports>>();
    }
}
