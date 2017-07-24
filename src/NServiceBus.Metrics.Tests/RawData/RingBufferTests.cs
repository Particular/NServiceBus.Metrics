namespace NServiceBus.Metrics.Tests.RawData
{
    using System.Linq;
    using System.Threading.Tasks;
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

        [Test]
        public void Full_write_multiple_times()
        {
            var values = Enumerable.Repeat(1, RingBuffer.Size).Select(i => (long)i).ToArray();
            WriteValues(values);
            Consume(values);
            WriteValues(values);
            Consume(values);
            WriteValues(values);
            Consume(values);
        }

        [Test]
        public void Estimate_just_initialized_ring_size()
        {
            // This test is single threaded so no cross-thread aberrations will be visible.

            Assert.AreEqual(0, ringBuffer.RoughlyEstimateItemsToConsume());
        }

        [Test]
        public void Estimate_empty_ring_size()
        {
            // This test is single threaded so no cross-thread aberrations will be visible.

            var values = Enumerable.Repeat(1, RingBuffer.Size).Select(i => (long)i).ToArray();
            WriteValues(values);
            Consume(values);

            Assert.AreEqual(0, ringBuffer.RoughlyEstimateItemsToConsume());
        }

        [Test]
        public void Estimate_full_ring_size()
        {
            // This test is single threaded so no cross-thread aberrations will be visible.

            var values = Enumerable.Repeat(1, RingBuffer.Size).Select(i => (long)i).ToArray();
            WriteValues(values);

            Assert.AreEqual(RingBuffer.Size, ringBuffer.RoughlyEstimateItemsToConsume());
        }

        [Test]
        public Task Concurrent_test()
        {
            const int size = 100000;

            var result = new byte[size * 2];

            var producer1 = Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < size; i++)
                {
                    while (ringBuffer.TryWrite(i) == false)
                    {
                    }
                }
            }, TaskCreationOptions.LongRunning);

            var producer2 = Task.Factory.StartNew(() =>
            {
                for (var i = 0; i < size; i++)
                {
                    while (ringBuffer.TryWrite(i + size) == false)
                    {
                    }
                }
            }, TaskCreationOptions.LongRunning);

            var consumer = Task.Factory.StartNew(() =>
            {

                while (true)
                {
                    int read;

                    // read till it returns
                    do
                    {
                        read = ringBuffer.Consume(chunk =>
                        {
                            foreach (var value in chunk)
                            {
                                result[value.Value] = 1;
                            }
                        });
                    } while (read > 0);

                    var completed = result.Count(b => b > 0);
                    if (completed == result.Length)
                    {
                        break;
                    }
                }
            });

            return Task.WhenAll(producer1, producer2, consumer);
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