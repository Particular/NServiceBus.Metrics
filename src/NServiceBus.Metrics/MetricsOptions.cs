namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::Metrics.Reporters;
    using Hosting;
    using Logging;
    using Metrics;
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
        /// <param name="interval">Reporting interval.</param>
        [Obsolete("Not for public use.")]
        public void SendMetricDataToServiceControl(TimeSpan interval, string serviceControlMetricsAddress)
        {
            Guard.AgainstNull(nameof(interval), interval);
            Guard.AgainstNullAndEmpty(nameof(serviceControlMetricsAddress), serviceControlMetricsAddress);

            ReportDefintions.Add(new ReportDefintion
            {
                Interval = interval,
                Builder = builder => new NServiceBusMetricReport(builder.Build<IDispatchMessages>(), serviceControlMetricsAddress, builder.Build<HostInformation>())
            });
        }

        /// <summary>
        /// Enables sending metric data to the trace log
        /// </summary>
        /// <param name="interval">Reporting interval.</param>
        public void EnableMetricTracing(TimeSpan interval)
        {
            Guard.AgainstNull(nameof(interval), interval);

            ReportDefintions.Add(new ReportDefintion
            {
                Interval = interval,
                Builder = builder => new TraceReport()
            });
        }

        /// <summary>
        /// Enables sending metric data to the NServiceBus log
        /// </summary>
        /// <param name="logLevel">Level at which log entries should be written. Default is DEBUG.</param>
        /// <param name="interval">Reporting interval.</param>
        public void EnableLogTracing(TimeSpan interval, LogLevel logLevel = LogLevel.Debug)
        {
            Guard.AgainstNull(nameof(interval), interval);

            ReportDefintions.Add(new ReportDefintion{
                Interval = interval,
                Builder = builder => new MetricsLogReport(logLevel)
            });
        }

        /// <summary>
        /// Enables custom report, allowing to consume data by any func.
        /// </summary>
        /// <param name="func">A function that will be called with a raw JSON.</param>
        /// <param name="interval">Reporting interval.</param>
        public void EnableCustomReport(TimeSpan interval, Func<string, Task> func)
        {
            Guard.AgainstNull(nameof(interval), interval);
            Guard.AgainstNull(nameof(func), func);

            ReportDefintions.Add(new ReportDefintion
            {
                Interval = interval,
                Builder = builder => new CustomReport(func)
            });
        }

        /// <summary>
        /// Registers consumer for raw measurements notifications.
        /// </summary>
        /// <param name="probesObserver">Probes observer. Receives array of available probes.</param>
        public void ObserverProbes(Action<Probe[]> probesObserver)
        {
            Guard.AgainstNull(nameof(probesObserver), probesObserver);

            ProbeObservers.Add(probesObserver);
        }

        internal List<ReportDefintion> ReportDefintions = new List<ReportDefintion>();
        internal List<Action<Probe[]>> ProbeObservers = new List<Action<Probe[]>>();

        internal class ReportDefintion
        {
            public TimeSpan Interval { get; set; }

            public Func<IBuilder, MetricsReport> Builder { get; set; }
        }
    }
}