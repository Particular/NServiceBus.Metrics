namespace NServiceBus.Metrics.Tests.RawData
{
    using System;
    using System.IO;
    using System.Text;
    using Metrics.RawData;
    using NUnit.Framework;

    public class TaggedLongValueWriterTests : WriterTestBase
    {
        static readonly UTF8Encoding NoBom = new UTF8Encoding(false);

        public TaggedLongValueWriterTests() 
            : base(Write)
        {
        }

        [Test]
        public void Writing_one_not_tagged_value()
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
                writer.Write(0); // tag count
                writer.Write(1); // entry count
                writer.Write(0);
                writer.Write(value);
            });
        }

        [Test]
        public void Writing_one_tagged_value()
        {
            const long version = 1L;
            const long ticks = 13123;
            const long value = 1345347;

            const string tagName = "this is a test tag value 111!!!";
            var tagId = Writer.GetTagId(tagName);
            var bytes = NoBom.GetBytes(tagName);

            var entry = new RingBuffer.Entry { Ticks = ticks, Value = value, Tag = tagId };
            Write(entry);

            Assert(writer =>
            {
                writer.Write(version);
                writer.Write(ticks);

                writer.Write(1); // tag count

                // tag entry
                writer.Write(tagId);
                writer.Write(bytes.Length);
                writer.Write(bytes);

                writer.Write(1); // entry count
                writer.Write(0);
                writer.Write(value);
            });
        }

        [Test]
        public void Writing_two_tagged_values_sorted_by_date()
        {
            const long version = 1L;
            const long ticks = 334346347;
            const long value1 = 983745897384573;
            const long value2 = value1 + 1;
            const int timeDiff = 1;

            const string tagName1 = "this is a test tag value 111!!!";
            var tagId1 = Writer.GetTagId(tagName1);
            var bytes1 = NoBom.GetBytes(tagName1);

            const string tagName2 = "this is another test tag value 222@@@ Even longer than the first one!";
            var tagId2 = Writer.GetTagId(tagName2);
            var bytes2 = NoBom.GetBytes(tagName2);

            Write(
                new RingBuffer.Entry(ticks + timeDiff, value2, tagId2),
                new RingBuffer.Entry(ticks, value1, tagId1)
            );

            Assert(writer =>
            {
                writer.Write(version);
                writer.Write(ticks);

                writer.Write(2); // tag count

                // tag1
                writer.Write(tagId1);
                writer.Write(bytes1.Length);
                writer.Write(bytes1); 
                
                // tag2
                writer.Write(tagId2);
                writer.Write(bytes2.Length);
                writer.Write(bytes2);

                writer.Write(2);
                writer.Write(0);
                writer.Write(value1);
                writer.Write(timeDiff);
                writer.Write(value2);
            });
        }



        [SetUp]
        public new void SetUp()
        {
             Writer = new TaggedLongValueWriter();
        }

        static void Write(BinaryWriter outputWriter, ArraySegment<RingBuffer.Entry> entries)
        {
            Writer.Write(outputWriter, entries);
        }

        static TaggedLongValueWriter Writer
        {
            get { return (TaggedLongValueWriter) TestContext.CurrentContext.Test.Properties.Get(nameof(TaggedLongValueWriter)); }
            set { TestContext.CurrentContext.Test.Properties.Set(nameof(TaggedLongValueWriter), value);}
        }
    }
}