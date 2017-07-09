namespace NServiceBus.Metrics.Tests.RawData
{
    using Metrics.RawData;
    using NUnit.Framework;

    public class OccurrenceWriterTests : WriterTestBase
    {
        public OccurrenceWriterTests()
            : base(OccurrenceWriter.Write)
        {
        }

        [Test]
        public void Writing_one_value()
        {
            const long version = 1L;
            const long ticks = 2;

            var entry = new RingBuffer.Entry { Ticks = ticks, Value = 23544345345 };
            Write(entry);

            Assert(writer =>
            {
                writer.Write(version);
                writer.Write(ticks);
                writer.Write(1);
                writer.Write(0);
            });
        }

        [Test]
        public void Writing_two_values_sorted_by_date()
        {
            const long version = 1L;
            const long ticks = 2;
            const int timeDiff = 1;

            Write(
                new RingBuffer.Entry(ticks + timeDiff, 8902374857238758343),
                new RingBuffer.Entry(ticks, 390489580934859034)
            );

            Assert(writer =>
            {
                writer.Write(version);
                writer.Write(ticks);
                writer.Write(2);
                writer.Write(0);
                writer.Write(timeDiff);
            });
        }
    }
}