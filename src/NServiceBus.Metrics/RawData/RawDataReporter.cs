namespace NServiceBus.Metrics.RawData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Hosting;
    using Logging;
    using Routing;
    using Support;
    using Transport;

    delegate void WriteOutput(ArraySegment<RingBuffer.Entry> entries, BinaryWriter outputWriter);

    class RawDataReporter : IDisposable
    {
        const int DefaultFlushSize = RingBuffer.Size / 2;
        readonly RingBuffer buffer;
        readonly int flushSize;
        readonly Action<ArraySegment<RingBuffer.Entry>> outputWriter;
        readonly IDispatchMessages dispatcher;
        readonly UnicastAddressTag destination;
        readonly TransportTransaction transportTransaction = new TransportTransaction();
        readonly Dictionary<string, string> headers = new Dictionary<string, string>();
        readonly BinaryWriter writer;
        readonly MemoryStream memoryStream;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly TimeSpan maxSpinningTime;
        Task reporter;

        static readonly TimeSpan DefaultMaxSpinningTime = TimeSpan.FromSeconds(5);
        static readonly TimeSpan singleSpinningTime = TimeSpan.FromMilliseconds(50);
        static ILog log = LogManager.GetLogger<RawDataReporter>();

        public RawDataReporter(IDispatchMessages dispatcher, string destination, HostInformation hostInformation, RingBuffer buffer, string messageTypeName, string endpointName,
            WriteOutput outputWriter) : this(dispatcher, destination, hostInformation, buffer, messageTypeName, endpointName, outputWriter, DefaultFlushSize, DefaultMaxSpinningTime)
        { }

        public RawDataReporter(IDispatchMessages dispatcher, string destination, HostInformation hostInformation, RingBuffer buffer, string messageTypeName, string endpointName,
            WriteOutput outputWriter, int flushSize, TimeSpan maxSpinningTime)
        {
            this.buffer = buffer;
            this.flushSize = flushSize;
            this.maxSpinningTime = maxSpinningTime;
            this.outputWriter = entries => outputWriter(entries, writer);
            this.dispatcher = dispatcher;
            this.destination = new UnicastAddressTag(destination);

            headers[Headers.OriginatingMachine] = RuntimeEnvironment.MachineName;
            headers[Headers.OriginatingHostId] = hostInformation.HostId.ToString("N");
            headers[Headers.EnclosedMessageTypes] = "NServiceBus.Metrics." + messageTypeName;
            headers[Headers.OriginatingEndpoint] = endpointName;
            headers[Headers.ContentType] = "LongValueOccurrence";

            memoryStream = new MemoryStream();
            writer = new BinaryWriter(memoryStream);
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            reporter = Task.Run(async () =>
            {
                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    var totalSpinningTime = TimeSpan.Zero;

                    // spin till either MaxSpinningTime is reached OR items to consume are more than FlushSize
                    while (totalSpinningTime < maxSpinningTime)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        var itemsToConsume = buffer.RoughlyEstimateItemsToConsume();
                        if (itemsToConsume >= flushSize)
                        {
                            break;
                        }

                        totalSpinningTime += singleSpinningTime;
                        await Task.Delay(singleSpinningTime).ConfigureAwait(false);
                    }

                    await Consume().ConfigureAwait(false);
                }

                // flush data before ending
                await Consume().ConfigureAwait(false);
            });
        }

        async Task Consume()
        {
            var consumed = buffer.Consume(outputWriter);

            if (consumed > 0)
            {
                writer.Flush();
                var body = memoryStream.ToArray(); // if only transport operation allowed ArraySegment<byte>...

                // clean stream
                memoryStream.SetLength(0);

                var message = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
                var operation = new TransportOperation(message, destination);
                try
                {
                    await dispatcher.Dispatch(new TransportOperations(operation), transportTransaction, new ContextBag()).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error($"Error while reporting raw data to {destination}.", ex);
                }
            }
        }

        public Task Stop()
        {
            cancellationTokenSource.Cancel();
            return reporter;
        }

        public void Dispose()
        {
            writer?.Dispose();
            memoryStream?.Dispose();
            cancellationTokenSource?.Dispose();
        }
    }
}