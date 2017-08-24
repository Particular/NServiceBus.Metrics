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
        void Register(Action<TimeSpan> observer);
    }
}