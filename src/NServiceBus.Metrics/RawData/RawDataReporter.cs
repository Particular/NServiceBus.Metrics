namespace NServiceBus.Metrics.RawData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Hosting;
    using Routing;
    using Support;
    using Transport;

    delegate void WriteOutput(ArraySegment<RingBuffer.Entry> entries, BinaryWriter outputWriter);

    class RawDataReporter
    {
        readonly RingBuffer buffer;
        readonly Action<ArraySegment<RingBuffer.Entry>> outputWriter;
        readonly IDispatchMessages dispatcher;
        readonly UnicastAddressTag destination;
        readonly TransportTransaction transportTransaction = new TransportTransaction();
        readonly Dictionary<string, string> headers = new Dictionary<string, string>();
        readonly BinaryWriter writer;
        readonly MemoryStream memoryStream;
        readonly CancellationTokenSource cancellationTokenSource;
        Task reporter;
        static readonly TimeSpan delayTime = TimeSpan.FromSeconds(1);

        public RawDataReporter(IDispatchMessages dispatcher, string destination, HostInformation hostInformation, RingBuffer buffer, string messageTypeName, string endpointName,
            WriteOutput outputWriter)
        {
            this.buffer = buffer;
            this.outputWriter = entries=>outputWriter(entries,writer);
            this.dispatcher = dispatcher;
            this.destination = new UnicastAddressTag(destination);

            headers[Headers.OriginatingMachine] = RuntimeEnvironment.MachineName;
            headers[Headers.OriginatingHostId] = hostInformation.HostId.ToString("N");
            headers[Headers.EnclosedMessageTypes] = "NServiceBus.Metrics." + messageTypeName; 
            headers[Headers.OriginatingEndpoint] = endpointName;

            memoryStream = new MemoryStream();
            writer = new BinaryWriter(memoryStream);
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            reporter = Task.Factory.StartNew(async () =>
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    // clear stream first
                    memoryStream.SetLength(0);

                    var consumed = buffer.Consume(outputWriter);

                    if (consumed == 0)
                    {
                        await Task.Delay(delayTime);
                    }
                    else
                    {
                        writer.Flush();
                        var body = memoryStream.ToArray(); // if only transport operation allowed ArraySegment<byte>...

                        var message = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
                        var operation = new TransportOperation(message, destination);
                        await dispatcher.Dispatch(new TransportOperations(operation), transportTransaction, new ContextBag()).ConfigureAwait(false);
                    }
                }
            });
        }

        public Task Stop()
        {
            cancellationTokenSource.Cancel();
            return reporter;
        }
    }
}