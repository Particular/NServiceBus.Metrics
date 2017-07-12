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
using NServiceBus.Hosting;
using NServiceBus.Logging;
using NServiceBus.Metrics;
using NServiceBus.Routing;
using NServiceBus.Support;
using NServiceBus.Transport;

class NServiceBusMetricReport : MetricsReport
{
    public NServiceBusMetricReport(IDispatchMessages dispatcher, MetricsOptions options, string endpointName, HostInformation hostInformation)
    {
        this.dispatcher = dispatcher;

        destination = new UnicastAddressTag(options.ServiceControlMetricsAddress);

        headers[Headers.OriginatingMachine] = RuntimeEnvironment.MachineName;
        headers[Headers.OriginatingHostId] = hostInformation.HostId.ToString("N");
        headers[Headers.EnclosedMessageTypes] = "NServiceBus.Metrics.MetricReport"; // without assembly name to allow ducktyping
        headers[Headers.ContentType] = ContentTypes.Json;
        headers[Headers.OriginatingEndpoint] = endpointName;
        headers[MetricHeaders.MetricInstanceId] = options.InstanceId;
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

        headers[Headers.OriginatingEndpoint] = metricsData.Context; // assumption that it will be always the endpoint name
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
    TransportTransaction transportTransaction = new TransportTransaction();

    Dictionary<string, string> headers = new Dictionary<string, string>();

    static ILog log = LogManager.GetLogger<NServiceBusMetricReport>();
}