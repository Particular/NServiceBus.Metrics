using System;

class ProcessingTimeCalculator
{
    public static TimeSpan Calculate(DateTime startedAt, DateTime completedAt) => completedAt - startedAt;
}