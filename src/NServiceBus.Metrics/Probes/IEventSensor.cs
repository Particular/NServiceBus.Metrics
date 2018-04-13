namespace NServiceBus
{
    /// <summary>
    /// A sensor for recording events
    /// </summary>
    /// <typeparam name="TEvent">The type of event being recorded</typeparam>
    public interface IEventSensor<TEvent> where TEvent : struct
    {
        /// <summary>
        /// Records an event
        /// </summary>
        /// <param name="e">The event</param>
        void Record(ref TEvent e);
    }
}