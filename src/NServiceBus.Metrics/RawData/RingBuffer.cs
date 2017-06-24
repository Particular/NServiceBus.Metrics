namespace NServiceBus.Metrics.RawData
{
    using System;
    using System.Threading;

    class RingBuffer
    {
        public const int Size = 4096;
        const long SizeMask = Size - 1;
        const long EpochMask = ~SizeMask;

        long nextToWrite;
        long nextToConsume;

        Entry[] entries = new Entry[Size];

        public struct Entry
        {
            public Entry(long ticks, long value)
            {
                Ticks = ticks;
                Value = value;
            }

            public long Ticks;
            public long Value;
        }

        public bool TryWrite(long value)
        {
            var readNextWrite = Volatile.Read(ref nextToWrite);
            long index;

            do
            {
                var consume = Volatile.Read(ref nextToConsume);

                // if writers overlaps with not consumed writes, end
                if (readNextWrite - consume >= Size)
                {
                    return false;
                }
                index = readNextWrite;

                // try to swap nextToWrite
                readNextWrite = Interlocked.CompareExchange(ref nextToWrite, index + 1, index);

                // do until the swap not succeeded
            } while (index != readNextWrite);
            
            // index is claimed, writing data
            var i = index & SizeMask;
            var ticks = DateTime.Now.Ticks;

            entries[i].Value = value;
            Volatile.Write(ref entries[i].Ticks, ticks);

            return true;
        }

        /// <summary>
        /// Consumes a chunk of entries. This method will call <paramref name="onChunk"/> zero, or one time. No multiple calls will be issued.
        /// </summary>
        /// <param name="onChunk"></param>
        public int Consume(Action<ArraySegment<Entry>> onChunk)
        {
            var consume = Interlocked.Read(ref nextToConsume);
            var max = Volatile.Read(ref nextToWrite);

            var i = consume;
            var epoch = i & EpochMask;
            var length = 0;
            while (Volatile.Read(ref entries[i & SizeMask].Ticks) > 0 && i < max)
            {
                if ((i & EpochMask) != epoch)
                    break;

                length++;
                i++;
            }

            // [consume, i) - entries to process
            var indexStart = (int)(consume & SizeMask);

            if (length == 0)
            {
                return 0;
            }

            onChunk(new ArraySegment<Entry>(entries, indexStart, length));
            Array.Clear(entries, indexStart, length);

            Interlocked.Add(ref nextToConsume, length);
            return length;
        }
    }
}