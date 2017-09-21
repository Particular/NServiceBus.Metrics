namespace NServiceBus.Metrics.QueueLength
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    class Context
    {
        readonly ConcurrentDictionary<string, Counter> counters = new ConcurrentDictionary<string, Counter>();
        readonly ConcurrentDictionary<Tuple<string,string>, Gauge> gauges = new ConcurrentDictionary<Tuple<string, string>, Gauge>();

        public IEnumerable<Counter> Counters => counters.Select(kvp => kvp.Value);
        public IEnumerable<Gauge> Gauges => gauges.Select(kvp => kvp.Value);

        public Counter Counter(string key) => counters.GetOrAdd(key, k => new Counter(k));
        public Gauge Gauge(string key, string queue) => gauges.GetOrAdd(Tuple.Create(key,queue), t => new Gauge(t.Item1,t.Item2));

        public string ToJson()
        {
            return NServiceBus.Metrics.SimpleJson.SerializeObject(this);
        }
    }

    class Gauge
    {
        long value;

        public Gauge(string key, string queue)
        {
            Tags = new[]
            {
                $"key:{key}",
                $"queue:{queue}",
                "type:queue-length.received"
            };
        }

        public void Report(long v)
        {
            Volatile.Write(ref value, v);
        }

        public double Value => Volatile.Read(ref value);

        public string[] Tags { get; }
    }

    class Counter
    {
        long value;

        public Counter(string key)
        {
            Tags = new[]
            {
                "type:queue-length.sent",
                $"key:{key}"
            };
        }

        public long Increment()
        {
            return Interlocked.Increment(ref value);
        }

        public long Count => Volatile.Read(ref value);
        public string[] Tags { get; }
    }

    //    const string Data = @"{
    //    ""Counters"": [{
    //        ""Name"": ""Sent sequence for sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"",
    //        ""Count"": 12,
    //        ""Unit"": ""Sequence"",
    //        ""Tags"": [""key:sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"",
    //        ""type:queue-length.sent""]
    //    }],
    //    ""Gauges"": [{
    //        ""Name"": ""Received sequence for sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"",
    //        ""Value"": 2.00,
    //        ""Unit"": ""Sequence"",
    //        ""Tags"": [""key:sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"",
    //        ""queue:Receiver@MyMachine"",
    //        ""type:queue-length.received""]
    //    }],
    //    ""Meters"": [],
    //    ""Timers"": []
    //}";
}