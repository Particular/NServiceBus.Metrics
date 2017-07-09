namespace NServiceBus.Metrics.RawData
{
    using System.Collections.Generic;

    class EntryTickComparer : IComparer<RingBuffer.Entry>
    {
        public static readonly EntryTickComparer Instance = new EntryTickComparer();

        EntryTickComparer() { }

        public int Compare(RingBuffer.Entry x, RingBuffer.Entry y)
        {
            return x.Ticks.CompareTo(y.Ticks);
        }
    }
}