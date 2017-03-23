using System;

namespace NServiceBus.Metrics
{
    class ProcessingTimeCalculator
    {
        public static TimeSpan Calculate(DateTime startedAt, DateTime completedAt) => completedAt - startedAt;
    }
}