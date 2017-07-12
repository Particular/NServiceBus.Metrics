using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Metrics;
using Metrics.Json;
using Metrics.MetricData;
using Metrics.Reporters;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Metrics;
using NServiceBus.Routing;
using NServiceBus.Transport;

class NServiceBusMetricReport : MetricsReport
{
    public NServiceBusMetricReport(IDispatchMessages dispatcher, MetricsOptions options, Dictionary<string, string> headers)
    {
        this.dispatcher = dispatcher;
        this.headers = headers;

        destination = new UnicastAddressTag(options.ServiceControlMetricsAddress);

    }

    public void RunReport(MetricsData metricsData, Func<HealthStatus> healthStatus, CancellationToken token)
    {
        RunReportAsync(metricsData)
            .IgnoreContinuation();
    }

    async Task RunReportAsync(MetricsData metricsData)
    {
        var stringBody = $@"{{""Data"" : {JsonBuilderV2.BuildJson(metricsData)}}}";
        var body = Encoding.UTF8.GetBytes(stringBody);

        var message = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
        var operation = new TransportOperation(message, destination);

        try
        {
            await dispatcher.Dispatch(new TransportOperations(operation), transportTransaction, new ContextBag())
                .ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            log.Error($"Error while sending metric data to {destination}.", exception);
        }
    }

    UnicastAddressTag destination;
    IDispatchMessages dispatcher;
    Dictionary<string, string> headers;
    TransportTransaction transportTransaction = new TransportTransaction();


    static ILog log = LogManager.GetLogger<NServiceBusMetricReport>();
}