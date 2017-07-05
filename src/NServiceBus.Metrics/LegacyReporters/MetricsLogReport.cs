using System;
using System.Linq;
using Metrics.Reporters;
using NServiceBus.Logging;

class MetricsLogReport : HumanReadableReport
{
    Action<string, object[]> logMethod;

    public MetricsLogReport(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Debug:
                logMethod = Log.DebugFormat;
                break;
            case LogLevel.Error:
                logMethod = Log.ErrorFormat;
                break;
            case LogLevel.Fatal:
                logMethod = Log.FatalFormat;
                break;
            case LogLevel.Info:
                logMethod = Log.InfoFormat;
                break;
            case LogLevel.Warn:
                logMethod = Log.WarnFormat;
                break;
        }
    }

    protected override void WriteLine(string line, params string[] args)
    {
        logMethod(line, args.Cast<object>().ToArray());
    }

    static ILog Log = LogManager.GetLogger<MetricsLogReport>();
}