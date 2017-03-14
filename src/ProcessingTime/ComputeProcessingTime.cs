namespace ProcessingTime
{
    /// <summary>
    /// Hooks into the NServiceBus pipeline and calculates Processing Time. This metric will be periodically sent to the Metrics Processing Component via a configured NServiceBus transport.
    /// </summary>
    public class ComputeProcessingTime
    {
        /// <summary>
        /// Dummy method to setup unit tests and build.
        /// </summary>
        /// <returns></returns>
        public bool IsWorking()
        {
            return true;
        }
    }
}
