namespace NServiceBus.Metrics.RawData
{
    using System;
    using System.IO;

    static class OccurrenceWriter
    {
        const long Version = 1;

        // WIRE FORMAT, Version: 1

        //0                   1                   2                   3
        //0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        //+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        //|     Version   | Base ticks    | date1 | date2 | date3 | date4 |  
        //+---------------+---------------+-------------------------------+
        //| date5 | date6 | date7 | date8 | date9 | ...
        //+---------------+---------------+-------------------------------+
        //| ....

        public static void Write(BinaryWriter outputWriter, ArraySegment<RingBuffer.Entry> chunk)
        {
            var array = chunk.Array;
            var offset = chunk.Offset;
            var count = chunk.Count;

            Array.Sort(array, offset, count, EntryTickComparer.Instance);

            var minDate = array[offset].Ticks;

            outputWriter.Write(Version);
            outputWriter.Write(minDate);

            for (var i = 0; i < count; i++)
            {
                // int allows to write ticks of 7minutes, as reporter runs much more frequent, this can be int
                var date = (int)(array[offset + i].Ticks - minDate);
                outputWriter.Write(date);
            }
        }
    }
}