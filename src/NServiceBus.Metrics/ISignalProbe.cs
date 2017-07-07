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
        void Register(Action observer);
    }
}