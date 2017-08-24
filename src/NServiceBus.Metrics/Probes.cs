namespace NServiceBus.Metrics
{
    /// <summary>
    /// This class provides identifiers for all the probes provided by this package.
    /// </summary>
    public static class Probes
    {
        /// <summary>
        /// A <see cref="ISignalProbe"/> that reports every time a message is pulled from a queue.
        /// </summary>
        public const string MessagePulled = "message-pulled";

        /// <summary>
        /// A <see cref="ISignalProbe"/> that reports every time a message processing fails.
        /// </summary>
        public const string MessageFailed = "message-failed";

        /// <summary>
        /// A <see cref="ISignalProbe"/> that reports every time a message is processed successfully.
        /// </summary>
        public const string MessageProcessed = "message-processed";

        /// <summary>
        /// A <see cref="ISignalProbe"/> that reports every time a message is scheduled for retry (FLR or SLR).
        /// </summary>
        public const string RetryOccurred = "retry-occurred";

        /// <summary>
        /// A <see cref="IDurationProbe"/> that reports the time it took from sending to processing the message.
        /// </summary>
        public const string CriticalTime = "critical-time";

        /// <summary>
        /// A <see cref="IDurationProbe"/> that reports the time it took to successfully process a message.
        /// </summary>
        public const string ProcessingTime = "processing-time";
    }
}