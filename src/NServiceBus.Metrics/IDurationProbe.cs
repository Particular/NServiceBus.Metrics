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

        /// <summary>
        /// Enables registering action called on event occurrence.
        /// </summary>
        [ObsoleteEx(Message = "Use Register with a DurationEvent instead", RemoveInVersion = "2.0.0")]
        void Register(Action<TimeSpan> observer);
    }

    /// <summary>
    /// Provides data for a single recorded duration.
    /// </summary>
    public struct DurationEvent
    {
        /// <summary>
        /// Creates the duration event.
        /// </summary>
        public DurationEvent(TimeSpan duration, string messageType)
        {
            Duration = duration;
            MessageType = messageType;
        }

        /// <summary>
        /// The duration value.
        /// </summary>
        public readonly TimeSpan Duration;

        /// <summary>
        /// The message type, the duration was recorded for, if any.
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        public readonly string MessageType;
    }
}