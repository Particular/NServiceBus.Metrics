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

    class RawDataReporter
    {
        public const long Version = 1;

        readonly RingBuffer buffer;
        readonly IDispatchMessages dispatcher;
        UnicastAddressTag destination;
        TransportTransaction transportTransaction = new TransportTransaction();
        Dictionary<string, string> headers = new Dictionary<string, string>();
        BinaryWriter writer;
        MemoryStream memoryStream;
        CancellationTokenSource cancellationTokenSource;
        Task reporter;
        static readonly TimeSpan delayTime = TimeSpan.FromSeconds(1);

        public RawDataReporter(IDispatchMessages dispatcher, string destination, HostInformation hostInformation, RingBuffer buffer)
        {
            this.buffer = buffer;
            this.dispatcher = dispatcher;
            this.destination = new UnicastAddressTag(destination);

            headers[Headers.OriginatingMachine] = RuntimeEnvironment.MachineName;
            headers[Headers.OriginatingHostId] = hostInformation.HostId.ToString("N");
            headers[Headers.EnclosedMessageTypes] = "NServiceBus.Metrics.RawData"; // without assembly name to allow ducktyping
            headers[Headers.ContentType] = ContentTypes.Json;
            headers[Headers.OriginatingEndpoint] = "SOMETHING";

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

                    var consumed = buffer.Consume(Consume);

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

        void Consume(ArraySegment<RingBuffer.Entry> chunk)
        {
            var array = chunk.Array;
            var offset = chunk.Offset;
            var count = chunk.Count;

            Array.Sort(array, offset, count, EntryTickComparer.Instance);

            var minDate = array[offset].Ticks;

            // WIRE FORMAT, Version: 1

            // 0                   1                   2                   3
            // 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
            //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            //|     Version   | Min date time | date1 |    value 1    | date2 |  
            //+---------------+---------------+-------------------------------+
            //|     value 2   | date3 |   value  3    | date4 |     value 4   |
            //+---------------+---------------+-------------------------------+
            

            writer.Write(Version);
            writer.Write(minDate);

            for (var i = 0; i < count; i++)
            {
                // int allows to write ticks of 7minutes, as reporter runs much more frequent, this can be int
                var date = (int)(array[offset + i].Ticks - minDate);
                writer.Write(date);
                writer.Write(array[offset + i].Value);
            }
        }

        class EntryTickComparer : IComparer<RingBuffer.Entry>
        {
            public static readonly EntryTickComparer Instance = new EntryTickComparer();

            EntryTickComparer() { }

            public int Compare(RingBuffer.Entry x, RingBuffer.Entry y)
            {
                return x.Ticks.CompareTo(y.Ticks);
            }
        }
    }
}