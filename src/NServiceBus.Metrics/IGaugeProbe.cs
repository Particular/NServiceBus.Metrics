namespace NServiceBus
{
    /// <summary>
    /// Probe that measures an exact value
    /// </summary>
    public interface IGaugeProbe : IProbe
    {
        /// <summary>
        /// Enables registering action called on event occurrence.
        /// </summary>
        void Register(OnEvent<GaugeEvent> observer);
    }

    /// <summary>
    /// Provides data for a single recording of the gauge
    /// </summary>
    public struct GaugeEvent
    {
        /// <summary>
        /// Creates the Gauge Event
        /// </summary>
        public GaugeEvent(long value, string tag)
        {
            Value = value;
            Tag = tag;
        }

        /// <summary>
        /// The measured value
        /// </summary>
        public readonly long Value;

        /// <summary>
        /// A tag to apply to this measurement, if any.
        /// </summary>
        public readonly string Tag;
    }
}