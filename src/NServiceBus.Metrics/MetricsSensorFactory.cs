using System.Collections.Generic;

namespace NServiceBus
{
    /// <summary>
    /// Factory for creating sensors
    /// </summary>
    public class MetricsSensorFactory
    {
        ICollection<IProbe> probes = new List<IProbe>();

        /// <summary>
        /// Creates a sensor for recording duration events
        /// </summary>
        public IEventSensor<DurationEvent> CreateDurationSensor(string name, string description)
        {
            var probe = new DurationProbe(name, description);
            probes.Add(probe);
            return probe;
        }

        /// <summary>
        /// Creates a sensor for recording signal events
        /// </summary>
        public IEventSensor<SignalEvent> CreateSignalSensor(string name, string description)
        {
            var probe = new SignalProbe(name, description);
            probes.Add(probe);
            return probe;
        }

        /// <summary>
        /// Creates a sensor for recording gauge readings
        /// </summary>
        public IEventSensor<GaugeEvent> CreateGaugeSensor(string name, string description)
        {
            var probe = new GaugeProbe(name, description);
            probes.Add(probe);
            return probe;
        }

        internal ProbeContext CreateProbeContext()
        {
            return new ProbeContext(probes);
        }

        internal void AddExisting(IProbe probe)
        {
            probes.Add(probe);
        }
    }
}
