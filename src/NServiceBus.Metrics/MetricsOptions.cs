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
        /// <param name="interval">Inteval between consequitive reports</param>
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

    /// <summary>
    /// Stores avialbe probes
    /// </summary>
    public class ProbeContext
    {
        internal ProbeContext(DurationProbe[] durations, SignalProbe[] signals)
        {
            Durations = durations;
            Signals = signals;
        }

        /// <summary>
        /// Duration type probes.
        /// </summary>
        public DurationProbe[] Durations { get; }

        /// <summary>
        /// Signal type probes.
        /// </summary>
        public SignalProbe[] Signals { get; }
    }

    /// <summary>
    /// Probe that signals event occurence
    /// </summary>
    public class SignalProbe : Probe
    {
        internal SignalProbe(string name, string description) : base(name, description)
        {
        }

        internal void Signal()
        {
            observers();
        }

        /// <summary>
        /// Enables registering action called on event occurence.
        /// </summary>
        /// <param name="observer"></param>
        public void Register(Action observer)
        {
            observers += observer;
        }

        Action observers = () => { };
    }

    /// <summary>
    /// Probe that measures duration of an event.
    /// </summary>
    public class DurationProbe : Probe
    {
        internal DurationProbe(string name, string description) : base(name, description)
        {
        }

        internal void Record(TimeSpan duration)
        {
            observers(duration);
        }

        /// <summary>
        /// Enables registering action called on event occurence.
        /// </summary>
        /// <param name="observer"></param>
        public void Register(Action<TimeSpan> observer)
        {
            observers += observer;
        }

        Action<TimeSpan> observers = span => { };
    }

    /// <summary>
    /// Enables receiving notifications after registering an observer
    /// </summary>
    public abstract class Probe
    {
        /// <summary>
        /// Name of the probe.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Descripton of the probe.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Creates <see cref="Probe"/>.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        protected Probe(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}