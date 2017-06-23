namespace NServiceBus.Metrics.Tests.RawData
{
    using System.Linq;
    using Metrics.RawData;
    using NUnit.Framework;

    public class RingBufferTests
    {
        RingBuffer ringBuffer;

        [SetUp]
        public void SetUp()
        {
            ringBuffer = new RingBuffer();
        }

        [Test]
        public void Writing_several_entries()
        {
            var values = new long[] { 1, 2, 3, 4 };

            WriteValues(values);

            Consume(values);
            Consume();
        }

        [Test]
        public void Writing_over_size_should_not_succeed()
        {
            var values = Enumerable.Repeat(1, RingBuffer.Size).Select(i => (long)i).ToArray();
            WriteValues(values);

            Assert.False(ringBuffer.TryWrite(long.MaxValue));

            Consume(values);
            Consume();
        }

        [Test]
        public void Overlapping_buffer_should_be_consumed_till_end_and_again()
        {
            var values = Enumerable.Repeat(1, RingBuffer.Size - 2).Select(i => (long)i).ToArray();
            WriteValues(values);
            Consume(values);
            Consume();

            WriteValues(1, 2, 3, 4);

            Consume(1, 2);
            Consume(3, 4);
            Consume();
        }

        void Consume(params long[] values)
        {
            var read = ringBuffer.Consume(entries =>
            {
                CollectionAssert.AreEqual(values, entries.Select(e => e.Value).ToArray());
            });

            Assert.AreEqual(values.Length, read);
        }

        void WriteValues(params long[] values)
        {
            foreach (var value in values)
            {
                Assert.True(ringBuffer.TryWrite(value));
            }
        }
    }
}