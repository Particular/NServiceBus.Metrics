using System;
using System.Threading;
using System.Threading.Tasks;
using Metrics;
using Metrics.Json;
using Metrics.MetricData;
using Metrics.Reporters;
using NServiceBus.Logging;
using NServiceBus.Metrics;

class CustomReport : MetricsReport
{
    public CustomReport(Func<string, Task> func)
    {
        this.func = func;
    }

    public void RunReport(MetricsData metricsData, Func<HealthStatus> healthStatus, CancellationToken token)
    {
        RunReportAsync(metricsData)
            .IgnoreContinuation();
    }

    async Task RunReportAsync(MetricsData metricsData)
    {
        var stringBody = JsonBuilderV2.BuildJson(metricsData);

        try
        {
            await func(stringBody).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            log.Error("Error while creating custom report.", exception);
        }
    }

    static ILog log = LogManager.GetLogger<CustomReport>();
    Func<string, Task> func;
}