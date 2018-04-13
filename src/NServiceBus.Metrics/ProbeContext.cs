namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Stores available probes
    /// </summary>
    public class ProbeContext
    {
        // TODO: Obsolete this constructor
        /// <summary>
        /// Creates <see cref="ProbeContext"/>.
        /// </summary>
        public ProbeContext(IReadOnlyCollection<IDurationProbe> durations, IReadOnlyCollection<ISignalProbe> signals)
        {
            Durations = durations;
            Signals = signals;
            Gauges = Enumerable.Empty<IGaugeProbe>().ToArray();
        }

        internal ProbeContext(ICollection<IProbe> probes)
        {
            Durations = probes.OfType<IDurationProbe>().ToArray();
            Signals = probes.OfType<ISignalProbe>().ToArray();
            Gauges = probes.OfType<IGaugeProbe>().ToArray();
        }

        /// <summary>
        /// Duration type probes.
        /// </summary>
        public IReadOnlyCollection<IDurationProbe> Durations { get; }

        /// <summary>
        /// Signal type probes.
        /// </summary>
        public IReadOnlyCollection<ISignalProbe> Signals { get; }

        /// <summary>
        /// Gauge type probes.
        /// </summary>
        public IReadOnlyCollection<IGaugeProbe> Gauges { get; }
    }
}