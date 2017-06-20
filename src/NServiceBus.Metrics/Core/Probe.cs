namespace NServiceBus.Metrics
{
    using System;

    /// <summary>
    /// Represent a source of measurement.
    /// </summary>
    public class Probe
    {
        /// <summary>
        /// Crates <see cref="Probe"/>
        /// </summary>
        /// <param name="name">Name of probe.</param>
        /// <param name="type">Type of value measured</param>
        /// <param name="description">Description of measurement.</param>
        public Probe(string name, MeasurementValueType type, string description)
        {
            Name = name;
            Type = type;
            Description = description;
        }

        /// <summary>
        /// Name of probe.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Type of value measured.
        /// </summary>
        public MeasurementValueType Type { get; }

        /// <summary>
        /// Description of probe.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Event called on each measurement.
        /// </summary>
        public event Action<long> MeasurementTaken;

        internal void Record(long value)
        {
            MeasurementTaken?.Invoke(value);
        }
    }

    /// <summary>
    /// Represents type of probe measurement
    /// </summary>
    public enum MeasurementValueType
    {
        /// <summary>
        /// Count.
        /// </summary>
        Count,

        /// <summary>
        /// Time.
        /// </summary>
        Time
    }
}