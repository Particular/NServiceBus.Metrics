using System.Linq;
using Metrics.Reporters;
using NServiceBus.Logging;

class MetricsLogReport : HumanReadableReport
{
    protected override void WriteLine(string line, params string[] args)
    {
        Log.InfoFormat(line, args.Cast<object>().ToArray());
    }

    static ILog Log = LogManager.GetLogger<MetricsLogReport>();
}