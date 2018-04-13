using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Support;
using NServiceBus.Transport;


class MsmqQueueLengthUpdater
{
    readonly IEventSensor<GaugeEvent> queueLength;
    readonly QueueBindings bindings;
    readonly List<Tuple<string, PerformanceCounter>> counters = new List<Tuple<string, PerformanceCounter>>();


    public MsmqQueueLengthUpdater(IEventSensor<GaugeEvent> queueLength, QueueBindings bindings)
    {
        this.queueLength = queueLength;
        this.bindings = bindings;
    }

    public void Warmup()
    {
        const string categoryName = "MSMQ Queue";
        const string counterName = "Messages in Queue";
        var machineName = RuntimeEnvironment.MachineName;

        counters.AddRange(
            from q in bindings.ReceivingAddresses
            let split = q.Split('@')
            let queueName = split.First()
            let instanceName = $@"{machineName}\private$\{queueName}".ToLowerInvariant()
            select Tuple.Create(
                q, 
                new PerformanceCounter(categoryName, counterName, instanceName, machineName)
            )
        );
    }

    public Task UpdateQueueLengths()
    {
        foreach (var counter in counters)
        {
            var @event = new GaugeEvent(TryGet(counter.Item2, -1), counter.Item1);
            queueLength.Record(ref @event);
        }

        return Task.FromResult(0);
    }

    static long TryGet(PerformanceCounter counter, long defaultValue)
    {
        try
        {
            return counter.RawValue;
        }
        catch(Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return defaultValue;
        }
    }
}
