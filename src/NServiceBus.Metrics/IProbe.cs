namespace NServiceBus
{
    /// <summary>
    /// Represents probe description.
    /// </summary>
    public interface IProbe
    {
        /// <summary>
        /// Name of the probe.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Descripton of the probe.
        /// </summary>
        string Description { get; }
    }
}