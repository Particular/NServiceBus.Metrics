namespace NServiceBus
{
    /// <summary>
    /// Probe that signals event occurrence
    /// </summary>
    public interface ISignalProbe : IProbe
    {
        /// <summary>
        /// Enables registering action called on event occurrence.
        /// </summary>
        void Register(OnEvent<SignalEvent> observer);
    }

    /// <summary>
    /// Provides data for a single occurrence of a signal.
    /// </summary>
    public readonly struct SignalEvent
    {
        /// <summary>
        /// Creates the signal event.
        /// </summary>
        public SignalEvent(string messageType) => MessageType = messageType;

        /// <summary>
        /// The message type, the duration was recorded for, if any.
        /// </summary>
        public readonly string MessageType;
    }
}