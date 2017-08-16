namespace NServiceBus
{
    /// <summary>
    /// Provides general information about a probe
    /// </summary>
    public interface IProbe
    {
        /// <summary>
        /// Provides an id of the probe.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// A human readable name of the probe.
        /// </summary>
        string Name { get; }
    }
}