using System;

namespace NServiceBus.Metrics.ProcessingTime
{
    class ProcessingTimeCalculator
    {
        public static TimeSpan Calculate(DateTime startedAt, DateTime completedAt) => completedAt - startedAt;
    }
}
