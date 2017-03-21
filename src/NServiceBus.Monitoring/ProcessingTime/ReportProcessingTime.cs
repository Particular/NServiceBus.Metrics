namespace NServiceBus.Monitoring.ProcessingTime
{
    using System;
    using System.Diagnostics;
    using Metrics;

    class ReportProcessingTime
    {
        public void Report(DateTime timeSent, string processedMessageType,  DateTime startedAt, DateTime completedAt)
        {
            var computedTimeElapsed = ProcessingTimeCalculator.Calculate(startedAt, completedAt);

            var processingTimeInMilliseconds = (long)computedTimeElapsed.TotalMilliseconds;

            //TODO - Report this info to Metrics.NET
            Trace.WriteLine($"Total time to process {processedMessageType} is {processingTimeInMilliseconds}");

            Metric.Counter("", Unit.Bytes);
        }
    }
}
