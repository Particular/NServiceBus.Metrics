namespace NServiceBus.Metrics.RawData
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// This writer provides a protocol for writing string-tagged values.
    /// The tags are written as a dictionary at the beginning of the payload, which enables reading it to a local dictionary and performing a fast transformation for the reader.
    /// </summary>
    class TaggedLongValueWriter
    {
        const long Version = 1;

        static readonly UTF8Encoding NoBom = new UTF8Encoding(false);
        readonly ConcurrentDictionary<string, Tag> byTagName = new ConcurrentDictionary<string, Tag>();
        readonly ConcurrentDictionary<int, Tag> byTagId = new ConcurrentDictionary<int, Tag>();
        readonly Func<string, Tag> GenerateTag;
        int generator;

        public TaggedLongValueWriter()
        {
            GenerateTag = str =>
            {
                var id = Interlocked.Increment(ref generator);
                var tag = new Tag(str, id);
                byTagId[id] = tag;
                return tag;
            };
        }

        // WIRE FORMAT, Version: 1

        // 0                   1                   2                   3
        // 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        //|     Version   | Min date time |tag cnt|tag1key|tag1len| tag1..|
        //+-------+-------+-------+-------+---------------+-------+-------+
        //| ..bytes       |tag2key|tag2len| tag2 bytes .................  |
        //+-------+-------+-------+-------+---------------+-------+-------+
        //| tag2 bytes continued                  | count | date1 | tag1  |
        //+-------+-------+-------+-------+---------------+-------+-------+
        //|    value 1    | date2 | tag2  |     value 2   | date3 | ...   |
        //+-------+---------------+-------+---------------+-------+-------+
        public void Write(BinaryWriter outputWriter, ArraySegment<RingBuffer.Entry> chunk)
        {
            var array = chunk.Array;
            var offset = chunk.Offset;
            var count = chunk.Count;

            Array.Sort(array, offset, count, EntryTickComparer.Instance);

            var minDate = array[offset].Ticks;

            outputWriter.Write(Version);
            outputWriter.Write(minDate);

            WriteTags(outputWriter, count, array, offset);

            outputWriter.Write(count);

            for (var i = 0; i < count; i++)
            {
                // int allows to write ticks of 7minutes, as reporter runs much more frequent, this can be int
                var date = (int)(array[offset + i].Ticks - minDate);
                outputWriter.Write(date);
                outputWriter.Write(array[offset + i].Value);
            }
        }

        void WriteTags(BinaryWriter outputWriter, int count, RingBuffer.Entry[] array, int offset)
        {
            var tags = new Dictionary<int, Tag>();
            for (var i = 0; i < count; i++)
            {
                var tag = array[offset + i].Tag;
                if (tag > 0 && tags.ContainsKey(tag) == false)
                {
                    tags[tag] = byTagId[tag];
                }
            }

            outputWriter.Write(tags.Count);
            foreach (var tag in tags)
            {
                outputWriter.Write(tag.Key);
                outputWriter.Write(tag.Value.Bytes.Length);
                outputWriter.Write(tag.Value.Bytes);
            }
        }

        public int GetTagId(string tagName) => byTagName.GetOrAdd(tagName, GenerateTag).ID;

        class Tag
        {
            public Tag(string tag, int id)
            {
                ID = id;
                Bytes = NoBom.GetBytes(tag);
            }

            public int ID { get; }
            public byte[] Bytes { get; }
        }
    }
}