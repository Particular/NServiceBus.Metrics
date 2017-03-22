namespace NServiceBus.Metrics
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using global::Metrics;
    using global::Metrics.Json;
    using global::Metrics.MetricData;
    using global::Metrics.Reporters;
    using Logging;
    using Routing;
    using Transport;

    class NServiceBusMetricReport : MetricsReport
    {
        string destination;
        IDispatchMessages dispatcher;

        public NServiceBusMetricReport(IDispatchMessages dispatcher, string destination)
        {
            this.dispatcher = dispatcher;
            this.destination = destination;
        }

        public void RunReport(MetricsData metricsData, Func<HealthStatus> healthStatus, CancellationToken token)
        {
            RunReportAsync(metricsData)
                .IgnoreContinuation();
        }

        async Task RunReportAsync(MetricsData metricsData)
        {
            var serialized = JsonBuilderV2.BuildJson(metricsData);
            var body = Encoding.UTF8.GetBytes(serialized);

            var headers = new Dictionary<string, string>
            {
                [Headers.EnclosedMessageTypes] = "Metrics.Json.JsonMetricsContext, Metrics"
            };
            var message = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
            var operation = new TransportOperation(message, new UnicastAddressTag(destination));

            try
            {
                await dispatcher.Dispatch(new TransportOperations(operation), transportTransaction, new ContextBag());
            }
            catch (Exception exception)
            {
                log.Error($"Error while sending metric data to {destination}.", exception);
            }
        }

        static ILog log = LogManager.GetLogger<NServiceBusMetricReport>();
        TransportTransaction transportTransaction = new TransportTransaction();
    }
}
