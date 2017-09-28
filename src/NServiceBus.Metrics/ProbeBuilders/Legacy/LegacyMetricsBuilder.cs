namespace NServiceBus.Metrics
{
    using System;

    abstract class MetricBuilder
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    class TimerAttribute : Attribute
    {
        public TimerAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public string Name { get; }

        public string Description { get; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    class MeterAttribute : Attribute
    {
        public MeterAttribute(string name, string description)
        {
            
        }
        public string Name { get; }

        public string Description { get; }
    }

    class LegacyMetricsBuilder : MetricBuilder
    {
#pragma warning disable 169
        [Timer("Processing Time", "The time it took to successfully process a message.")]
        public object ProcessingTimeTimer;

        [Timer("Critical Time", "The time it took from sending to processing the message.")]
        public object CriticalTimeTimer;

        [Meter("# of messages pulled from the input queue / sec", "The current number of messages pulled from the input queue by the transport per second.")]
        public object MessagesPulledFromQueueMeter;

        [Meter("# of message failures / sec", "The current number of failed processed messages by the transport per second.")]
        public object FailureRateMeter;

        [Meter("# of messages successfully processed / sec", "The current number of messages processed successfully by the transport per second.")]
        public object SuccessRateMeter;
#pragma warning restore 169
    }
}