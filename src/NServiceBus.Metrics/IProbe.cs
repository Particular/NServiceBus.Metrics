namespace NServiceBus
{
    /// <summary>
    /// Provides general information about a probe
    /// </summary>
    public interface IProbe
    {
        /// <summary>
        /// Name of the probe.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of the probe.
        /// </summary>
        string Description { get; }
    }
}