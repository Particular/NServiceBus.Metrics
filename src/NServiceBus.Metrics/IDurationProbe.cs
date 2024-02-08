namespace NServiceBus
{
    using System;

    /// <summary>
    /// Probe that measures duration of an event.
    /// </summary>
    public interface IDurationProbe : IProbe
    {
        /// <summary>
        /// Enables registering action called on event occurrence.
        /// </summary>
        void Register(OnEvent<DurationEvent> observer);
    }

    /// <summary>
    /// Provides data for a single recorded duration.
    /// </summary>
    public struct DurationEvent(TimeSpan duration, string messageType)
    {

        /// <summary>
        /// The duration value.
        /// </summary>
        public readonly TimeSpan Duration = duration;

        /// <summary>
        /// The message type, the duration was recorded for, if any.
        /// </summary>
        public readonly string MessageType = messageType;
    }
}