namespace NServiceBus.Metrics.Tests.RawData
{
    using System;
    using System.IO;
    using System.Linq;
    using Metrics.RawData;
    using NUnit.Framework;

    public abstract class WriterTestBase : IDisposable
    {
        MemoryStream ms;
        BinaryWriter bw;
        Action<BinaryWriter, ArraySegment<RingBuffer.Entry>> writer;

        internal WriterTestBase(Action<BinaryWriter, ArraySegment<RingBuffer.Entry>> writer)
        {
            ms = new MemoryStream();
            bw = new BinaryWriter(ms);
            this.writer = writer;
        }

        [SetUp]
        public void SetUp()
        {
            bw.Flush();
            ms.SetLength(0);
        }

        internal void Write(params RingBuffer.Entry[] entries)
        {
            // put additional values in front and at the end to make it a real segment
            var list = entries.ToList();
            list.Insert(0, new RingBuffer.Entry());
            list.Add(new RingBuffer.Entry());

            writer(bw, new ArraySegment<RingBuffer.Entry>(list.ToArray(), 1, entries.Length));

            bw.Flush();
        }

        protected void Assert(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(stream))
                {
                    write(binaryWriter);
                    binaryWriter.Flush();
                }

                CollectionAssert.AreEqual(stream.ToArray(), ms.ToArray());
            }
        }

        public void Dispose()
        {
            bw?.Dispose();
            ms?.Dispose();
        }
    }
}