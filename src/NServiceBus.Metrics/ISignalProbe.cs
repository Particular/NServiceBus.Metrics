namespace NServiceBus
{
    using System;

    /// <summary>
    /// Probe that signals event occurrence
    /// </summary>
    public interface ISignalProbe : IProbe
    {
        /// <summary>
        /// Enables registering action called on event occurrence.
        /// </summary>
        void Register(OnEvent<SignalEvent> observer);

        /// <summary>
        /// Enables registering action called on event occurrence.
        /// </summary>
        [ObsoleteEx(Message = "Use Register with a DurationEvent instead", RemoveInVersion = "2.0.0")]
        void Register(Action observer);
    }

    /// <summary>
    /// Provides data for a single occurrence of a signal.
    /// </summary>
    public struct SignalEvent
    {
        /// <summary>
        /// Creates the signal event.
        /// </summary>
        public SignalEvent(string messageType)
        {
            MessageType = messageType;
        }

        /// <summary>
        /// The message type, the duration was recorded for, if any.
        /// </summary>
        public readonly string MessageType;
    }
}