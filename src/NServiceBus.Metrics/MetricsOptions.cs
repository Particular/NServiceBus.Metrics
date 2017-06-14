namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::Metrics.Reporters;
    using Hosting;
    using Logging;
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
        [Obsolete("Not for public use.")]
        public void SendMetricDataToServiceControl(string serviceControlMetricsAddress)
        {
            Guard.AgainstNullAndEmpty(nameof(serviceControlMetricsAddress), serviceControlMetricsAddress);

            ReportBuilders.Add(builder => new NServiceBusMetricReport(builder.Build<IDispatchMessages>(), serviceControlMetricsAddress, builder.Build<HostInformation>()));
        }

        /// <summary>
        /// Enables sending metric data to the trace log
        /// </summary>
        public void EnableMetricTracing()
        {
            ReportBuilders.Add(builder =>  new TraceReport());
        }

        /// <summary>
        /// Enables sending metric data to the NServiceBus log
        /// </summary>
        /// <param name="logLevel">Level at which log entries should be written. Default is DEBUG.</param>
        public void EnableLogTracing(LogLevel logLevel = LogLevel.Debug)
        {
            ReportBuilders.Add(builder =>  new MetricsLogReport(logLevel));
        }

        /// <summary>
        /// Enables custom report, allowing to consume data by any func.
        /// </summary>
        /// <param name="func">A function that will be called with a raw JSON.</param>
        public void EnableCustomReport(Func<string, Task> func)
        {
            Guard.AgainstNull(nameof(func), func);

            ReportBuilders.Add(builder => new CustomReport(func));
        }

        internal void ReportInterval(TimeSpan interval)
        {
            ReportingInterval = interval;
        }

        internal List<Func<IBuilder, MetricsReport>> ReportBuilders = new List<Func<IBuilder, MetricsReport>>();

        internal TimeSpan ReportingInterval = TimeSpan.FromSeconds(30);
    }
}