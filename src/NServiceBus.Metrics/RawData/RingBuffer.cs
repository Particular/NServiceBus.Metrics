namespace NServiceBus.Metrics.RawData
{
    using System;
    using System.Threading;

    class RingBuffer
    {
        const long Size = 65536;
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

            if (write - consume > Size)
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
            while (Volatile.Read(ref entries[i].Ticks) > 0 && i < max)
            {
                i++;
            }

            // [consume, i) - entries to process
            var indexStart = (int)(consume & SizeMask);
            var length = (int)(i - consume);

            if (length == 0)
            {
                return length;
            }

            var startEpoch = indexStart & EpochMask;
            var endEpoch = (indexStart + length) & EpochMask;

            if (startEpoch == endEpoch)
            {
                onChunk(new ArraySegment<Entry>(entries, indexStart, length));
                Array.Clear(entries, indexStart, length);
            }
            else
            {
                // buffer overlaps
                var firstEpochEnd = startEpoch + Size - 1;
                var firstEpochLength = (int)(consume - firstEpochEnd);
                onChunk(new ArraySegment<Entry>(entries, indexStart, firstEpochLength));
                Array.Clear(entries, indexStart, firstEpochLength);

                // onChunk is called once per call now
                //onChunk(new ArraySegment<Entry>(entries, 0, length - firstEpochLength));
                //Array.Clear(entries, 0, length - firstEpochLength);
            }

            Interlocked.Add(ref nextToConsume, length);
            return length;
        }
    }
}