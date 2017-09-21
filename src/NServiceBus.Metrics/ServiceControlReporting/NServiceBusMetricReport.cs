using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Metrics.QueueLength;
using NServiceBus.Routing;
using NServiceBus.Transport;

class NServiceBusMetricReport
{
    public NServiceBusMetricReport(IDispatchMessages dispatcher, MetricsOptions options, Dictionary<string, string> headers, Context context)
    {
        this.dispatcher = dispatcher;
        this.headers = headers;
        this.context = context;

        destination = new UnicastAddressTag(options.ServiceControlMetricsAddress);
    }

    public async Task RunReportAsync()
    {
        var stringBody = $@"{{""Data"" : {context.ToJson()}}}";
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
    readonly Context context;
    TransportTransaction transportTransaction = new TransportTransaction();

    static ILog log = LogManager.GetLogger<NServiceBusMetricReport>();
}