namespace NServiceBus.Metrics.Tests.RawData
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using Hosting;
    using Metrics.RawData;
    using NUnit.Framework;
    using Transport;

    public class RawDataReporterTests
    {
        static readonly HostInformation HostInformation = new HostInformation(Guid.NewGuid(), "display-name");
        RingBuffer buffer;
        MockDispatcher dispatcher;
        const string Destination = "destination";
        const string MessageTypeName = "message.type.name";
        const string EndpointName = "endpoint.name";

        [SetUp]
        public void SetUp()
        {
            buffer = new RingBuffer();
            dispatcher = new MockDispatcher();
        }

        [Test]
        public async Task When_flush_size_is_reached()
        {
            var reporter = new RawDataReporter(dispatcher, Destination, HostInformation, buffer, MessageTypeName, EndpointName, WriteEntriesValues, 4, TimeSpan.MaxValue);
            reporter.Start();
            buffer.TryWrite(1);
            buffer.TryWrite(2);
            buffer.TryWrite(3);
            buffer.TryWrite(4);
            
            Assert(new long[]{1,2,3,4});

            await reporter.Stop();
        }

        [Test]
        public async Task When_max_spinning_time_is_reached()
        {
            var maxSpinningTime = TimeSpan.FromMilliseconds(100);

            var reporter = new RawDataReporter(dispatcher, Destination, HostInformation, buffer, MessageTypeName, EndpointName, WriteEntriesValues, int.MaxValue, maxSpinningTime);
            reporter.Start();
            buffer.TryWrite(1);
            buffer.TryWrite(2);
            await Task.Delay(maxSpinningTime.Add(TimeSpan.FromMilliseconds(200)));
            
            Assert(new long[] { 1, 2 });

            await reporter.Stop();
        }

        [Test]
        public async Task When_stopped()
        {
            var reporter = new RawDataReporter(dispatcher, Destination, HostInformation, buffer, MessageTypeName, EndpointName, WriteEntriesValues, int.MaxValue, TimeSpan.MaxValue);
            reporter.Start();
            buffer.TryWrite(1);
            buffer.TryWrite(2);
            
            await reporter.Stop();

            Assert(new long[] { 1, 2 });
        }

        void Assert(params long[][] values)
        {
            var operations = dispatcher.Operations.ToArray();
            var i = 0;
            foreach (var operation in operations)
            {
                var op = operation.UnicastTransportOperations.Single();

                var encodedValues = new List<long>();
                var reader = new BinaryReader(new MemoryStream(op.Message.Body));
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    encodedValues.Add(reader.ReadInt64());
                }

                CollectionAssert.AreEqual(values[i], encodedValues);
            }
        }

        static void WriteEntriesValues(ArraySegment<RingBuffer.Entry> entries, BinaryWriter writer)
        {
            foreach (var entry in entries)
            {
                writer.Write(entry.Value);
            }
        }

        class MockDispatcher : IDispatchMessages
        {
            public ConcurrentQueue<TransportOperations> Operations = new ConcurrentQueue<TransportOperations>();

            public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
            {
                Operations.Enqueue(outgoingMessages);
                return Task.FromResult(0);
            }
        }
    }
}
