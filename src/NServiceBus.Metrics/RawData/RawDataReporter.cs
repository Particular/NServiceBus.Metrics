namespace NServiceBus.Metrics.RawData
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using Logging;
    using Transports;
    using Unicast;

    delegate void WriteOutput(ArraySegment<RingBuffer.Entry> entries, BinaryWriter outputWriter);

    class RawDataReporter : IDisposable
    {
        const int DefaultFlushSize = RingBuffer.Size / 2;
        readonly RingBuffer buffer;
        readonly int flushSize;
        readonly Action<ArraySegment<RingBuffer.Entry>> outputWriter;
        readonly ISendMessages dispatcher;
        readonly string destination;
        readonly Dictionary<string, string> headers;
        readonly BinaryWriter writer;
        readonly MemoryStream memoryStream;
        readonly TimeSpan maxSpinningTime;

        static readonly TimeSpan DefaultMaxSpinningTime = TimeSpan.FromSeconds(5);
        static readonly TimeSpan singleSpinningTime = TimeSpan.FromMilliseconds(50);
        static ILog log = LogManager.GetLogger<RawDataReporter>();
        Thread reportingThread;
        volatile bool isRunning = true;

        public RawDataReporter(ISendMessages dispatcher, string destination, Dictionary<string, string> headers, RingBuffer buffer, WriteOutput outputWriter) 
            : this(dispatcher, destination, headers, buffer, outputWriter, DefaultFlushSize, DefaultMaxSpinningTime)
        { }

        public RawDataReporter(ISendMessages dispatcher, string destination, Dictionary<string, string> headers, RingBuffer buffer, WriteOutput outputWriter, int flushSize, TimeSpan maxSpinningTime)
        {
            this.buffer = buffer;
            this.flushSize = flushSize;
            this.maxSpinningTime = maxSpinningTime;
            this.outputWriter = entries => outputWriter(entries, writer);
            this.dispatcher = dispatcher;
            this.destination = destination;
            this.headers = headers;

            memoryStream = new MemoryStream();
            writer = new BinaryWriter(memoryStream);
        }

        public void Start()
        {
            reportingThread = new Thread(() =>
            {
                while (isRunning)
                {
                    var totalSpinningTime = TimeSpan.Zero;

                    // spin till either MaxSpinningTime is reached OR items to consume are more than FlushSize
                    while (totalSpinningTime < maxSpinningTime)
                    {
                        if (isRunning == false)
                        {
                            break;
                        }

                        var itemsToConsume = buffer.RoughlyEstimateItemsToConsume();
                        if (itemsToConsume >= flushSize)
                        {
                            break;
                        }

                        totalSpinningTime += singleSpinningTime;
                        Thread.Sleep(singleSpinningTime);
                    }

                    Consume();
                }

                // flush data before ending
                Consume();
            });
        }

        void Consume()
        {
            var consumed = buffer.Consume(outputWriter);

            if (consumed > 0)
            {
                writer.Flush();
                var body = memoryStream.ToArray(); // if only transport operation allowed ArraySegment<byte>...

                // clean stream
                memoryStream.SetLength(0);

                try
                {
                    dispatcher.Send(new TransportMessage(Guid.NewGuid().ToString(), headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
                    {
                        Body = body,

                    }, new SendOptions(destination));
                }
                catch (Exception ex)
                {
                    log.Error($"Error while reporting raw data to {destination}.", ex);
                }
            }
        }

        public void Stop()
        {
            isRunning = false;
            reportingThread.Join();
        }

        public void Dispose()
        {
            writer?.Dispose();
            memoryStream?.Dispose();
        }
    }
}