namespace NServiceBus
{
    using System.Collections.Generic;

    /// <summary>
    /// Stores available probes
    /// </summary>
    public class ProbeContext
    {
        /// <summary>
        /// Creates <see cref="ProbeContext"/>.
        /// </summary>
        public ProbeContext(IReadOnlyCollection<IDurationProbe> durations, IReadOnlyCollection<ISignalProbe> signals)
        {
            Durations = durations;
            Signals = signals;
        }

        /// <summary>
        /// Duration type probes.
        /// </summary>
        public IReadOnlyCollection<IDurationProbe> Durations { get; }

        /// <summary>
        /// Signal type probes.
        /// </summary>
        public IReadOnlyCollection<ISignalProbe> Signals { get; }
    }
}