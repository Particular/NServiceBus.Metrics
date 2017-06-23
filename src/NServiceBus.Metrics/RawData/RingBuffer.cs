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
            public long Ticks;
            public long Value;
        }

        public bool TryWrite(long value)
        {
            var write = Interlocked.Increment(ref nextToWrite) - 1;
            var consume = Volatile.Read(ref nextToConsume);

            if (write - consume >= Size)
            {
                return false;
            }

            var i = write & SizeMask;
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