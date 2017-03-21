namespace NServiceBus.Metrics.ProcessingTime
{
    using System.Diagnostics;
    using global::Metrics.Reporters;

    class TraceReport : HumanReadableReport
    {
        protected override void WriteLine(string line, params string[] args)
        {
            Trace.WriteLine(string.Format(line, args));
        }
    }
}
