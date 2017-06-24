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
        /// <summary>
        /// Crates <see cref="ProbeContext"/>.
        /// </summary>
        /// <param name="durations">Duration probes collection.</param>
        /// <param name="signals">Signal probes collection</param>
        public ProbeContext(IDurationProbe[] durations, ISignalProbe[] signals)
        {
            Durations = durations;
            Signals = signals;
        }

        /// <summary>
        /// Duration type probes.
        /// </summary>
        public IDurationProbe[] Durations { get; }

        /// <summary>
        /// Signal type probes.
        /// </summary>
        public ISignalProbe[] Signals { get; }
    }

    /// <summary>
    /// Probe that signals event occurence
    /// </summary>
    public interface ISignalProbe
    {
        /// <summary>
        /// Enables registering action called on event occurence.
        /// </summary>
        /// <param name="observer"></param>
        void Register(Action observer);

        /// <summary>
        /// Name of the probe.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Descripton of the probe.
        /// </summary>
        string Description { get; }
    }

    
    class SignalProbe : Probe, ISignalProbe
    {
        public SignalProbe(string name, string description) : base(name, description)
        {
        }

        internal void Signal()
        {
            observers();
        }

        public void Register(Action observer)
        {
            observers += observer;
        }

        Action observers = () => { };
    }

    /// <summary>
    /// Probe that measures duration of an event.
    /// </summary>
    public interface IDurationProbe
    {
        /// <summary>
        /// Enables registering action called on event occurence.
        /// </summary>
        /// <param name="observer"></param>
        void Register(Action<TimeSpan> observer);

        /// <summary>
        /// Name of the probe.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Descripton of the probe.
        /// </summary>
        string Description { get; }
    }

    class DurationProbe : Probe, IDurationProbe
    {
        public DurationProbe(string name, string description) : base(name, description)
        {
        }

        internal void Record(TimeSpan duration)
        {
            observers(duration);
        }

        public void Register(Action<TimeSpan> observer)
        {
            observers += observer;
        }

        Action<TimeSpan> observers = span => { };
    }

    abstract class Probe
    {
        public string Name { get; }

        public string Description { get; }

        protected Probe(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}