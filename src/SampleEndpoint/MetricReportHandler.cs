using System;
using System.Threading.Tasks;
using Metrics.Json;
using NServiceBus;
using NServiceBus.Metrics;

class MetricReportHandler : IHandleMessages<MetricReport>
{
    public Task Handle(MetricReport message, IMessageHandlerContext context)
    {
        var metricsData = message.Data.ToObject<JsonMetricsContext>();
        Console.WriteLine(metricsData.Context);
        return Task.CompletedTask;
    }
}