namespace NServiceBus
{
    using System.Collections.Generic;

    /// <summary>
    /// Stores available probes
    /// </summary>
    public class ProbeContext(IReadOnlyCollection<IDurationProbe> durations, IReadOnlyCollection<ISignalProbe> signals)
    {
        /// <summary>
        /// Duration type probes.
        /// </summary>
        public IReadOnlyCollection<IDurationProbe> Durations { get; } = durations;

        /// <summary>
        /// Signal type probes.
        /// </summary>
        public IReadOnlyCollection<ISignalProbe> Signals { get; } = signals;
    }
}