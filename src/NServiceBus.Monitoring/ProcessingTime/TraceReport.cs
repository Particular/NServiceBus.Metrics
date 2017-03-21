namespace NServiceBus.Monitoring.ProcessingTime
{
    using System.Diagnostics;
    using Metrics.Reporters;

    class TraceReport : HumanReadableReport
    {
        protected override void WriteLine(string line, params string[] args)
        {
            Trace.WriteLine(string.Format(line, args));
        }
    }
}