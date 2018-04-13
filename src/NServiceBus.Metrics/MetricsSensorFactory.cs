using System.Collections.Generic;
using System.Linq;

namespace NServiceBus
{
    /// <summary>
    /// Factory for creating sensors
    /// </summary>
    public class MetricsSensorFactory
    {
        IList<Probe> probes = new List<Probe>();

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

        internal ProbeContext CreateProbeContext()
        {
            return new ProbeContext(
                probes.OfType<IDurationProbe>().ToList().AsReadOnly(),
                probes.OfType<ISignalProbe>().ToList().AsReadOnly()
            );
        }

        internal void AddExisting(Probe probe)
        {
            probes.Add(probe);
        }
    }
}
