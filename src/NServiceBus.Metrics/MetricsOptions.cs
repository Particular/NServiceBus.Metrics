namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provides configuration options for Metrics feature
    /// </summary>
    public class MetricsOptions
    {
        /// <summary>
        /// Enables sending periodic updates of metric data to ServiceControl
        /// </summary>
        /// <param name="serviceControlMetricsAddress">The transport address of the ServiceControl instance</param>
        /// <param name="interval">Interval between consecutive reports</param>
        [Obsolete("Not for public use.")]
        public void SendMetricDataToServiceControl(string serviceControlMetricsAddress, TimeSpan interval)
        {
            Guard.AgainstNullAndEmpty(nameof(serviceControlMetricsAddress), serviceControlMetricsAddress);
            Guard.AgainstNegativeAndZero(nameof(interval), interval);

            ServiceControlMetricsAddress = serviceControlMetricsAddress;
            ReportingInterval = interval;
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

        internal string ServiceControlMetricsAddress;
        internal TimeSpan ReportingInterval;

        Action<ProbeContext> registerObservers = c => {};
    }
}