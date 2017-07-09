namespace NServiceBus.Metrics.Tests.RawData
{
    using Metrics.RawData;
    using NUnit.Framework;

    public class LongValueWriterTests : WriterTestBase
    {
        public LongValueWriterTests() 
            : base(LongValueWriter.Write)
        {
        }

        [Test]
        public void Writing_one_value()
        {
            const long version = 1L;
            const long ticks = 2;
            const long value = 3;

            var entry = new RingBuffer.Entry { Ticks = ticks, Value = value };
            Write(entry);

            Assert(writer =>
            {
                writer.Write(version);
                writer.Write(ticks);
                writer.Write(1);
                writer.Write(0);
                writer.Write(value);
            });
        }

        [Test]
        public void Writing_two_values_sorted_by_date()
        {
            const long version = 1L;
            const long ticks = 2;
            const long value1 = 3;
            const long value2 = value1 + 1;
            const int timeDiff = 1;

            Write(
                new RingBuffer.Entry(ticks + timeDiff, value2),
                new RingBuffer.Entry(ticks, value1)
                );

            Assert(writer =>
            {
                writer.Write(version);
                writer.Write(ticks);
                writer.Write(2);
                writer.Write(0);
                writer.Write(value1);
                writer.Write(timeDiff);
                writer.Write(value2);
            });
        }
    }
}