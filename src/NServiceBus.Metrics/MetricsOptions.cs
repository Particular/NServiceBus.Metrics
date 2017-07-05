namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::Metrics;
    using global::Metrics.Reports;
    using Logging;

    /// <summary>
    /// Provides configuration options for Metrics feature
    /// </summary>
    public class MetricsOptions
    {
        /// <summary>
        /// Enables sending metric data to the trace log
        /// </summary>
        /// <param name="interval">How often metric data is sent to the trace log</param>
        [ObsoleteEx(RemoveInVersion = "3.0", TreatAsErrorFromVersion = "3.0", Message = "Use RegisterObservers instead to attach to monitoring probes.")]
        public void EnableMetricTracing(TimeSpan interval)
        {
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            legacyReportInstallers.Add(config => config.WithReport(
                new TraceReport(),
                interval
            ));
        }

        /// <summary>
        /// Enables sending metric data to the NServiceBus log
        /// </summary>
        /// <param name="interval">How often metric data is sent to the log</param>
        /// <param name="logLevel">Level at which log entries should be written. Default is DEBUG.</param>
        [ObsoleteEx(RemoveInVersion = "3.0", TreatAsErrorFromVersion = "3.0", Message = "Use RegisterObservers instead to attach to monitoring probes.")]
        public void EnableLogTracing(TimeSpan interval, LogLevel logLevel = LogLevel.Debug)
        {
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            legacyReportInstallers.Add(config => config.WithReport(
                new MetricsLogReport(logLevel),
                interval
            ));
        }

        /// <summary>
        /// Enables custom report, allowing to consume data by any func.
        /// </summary>
        /// <param name="func">A function that will be called with a raw JSON.</param>
        /// <param name="interval">How often metric data is sent to the log</param>
        [ObsoleteEx(RemoveInVersion = "3.0", TreatAsErrorFromVersion = "3.0", Message = "Use RegisterObservers instead to attach to monitoring probes.")]
        public void EnableCustomReport(Func<string, Task> func, TimeSpan interval)
        {
            Guard.AgainstNull(nameof(func), func);
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            legacyReportInstallers.Add(config => config.WithReport(
                new CustomReport(func),
                interval
            ));
        }

        /// <summary>
        /// Enables sending periodic updates of metric data to ServiceControl
        /// </summary>
        /// <param name="serviceControlMetricsAddress">The transport address of the ServiceControl instance</param>
        /// <param name="interval">Interval between consecutive reports</param>
        [ObsoleteEx(Message = "Not for public use.")]
        public void SendMetricDataToServiceControl(string serviceControlMetricsAddress, TimeSpan interval)
        {
            Guard.AgainstNullAndEmpty(nameof(serviceControlMetricsAddress), serviceControlMetricsAddress);
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            ServiceControlMetricsAddress = serviceControlMetricsAddress;
            ServiceControlReportingInterval = interval;
        }

        /// <summary>
        /// Enables registering observers to available probes.
        /// </summary>
        /// <param name="register">Action that registers observers to probes</param>
        public void RegisterObservers(Action<ProbeContext> register)
        {
            Guard.AgainstNull(nameof(register), register);

            registerObservers += register;
        }

        internal void SetUpObservers(ProbeContext probeContext)
        {
            registerObservers(probeContext);
        }

        internal void SetUpLegacyReports(MetricsConfig config)
        {
            config.WithReporting(reportsConfig => legacyReportInstallers.ForEach(installer => installer(reportsConfig)));
        }

        internal string ServiceControlMetricsAddress;
        internal TimeSpan ServiceControlReportingInterval;

        Action<ProbeContext> registerObservers = c => {};

        List<Action<MetricsReports>> legacyReportInstallers = new List<Action<MetricsReports>>();
    }
}