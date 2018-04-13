using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Transport;


class MsmqQueueLengthUpdater
{
    static readonly ILog Log = LogManager.GetLogger<MsmqQueueLengthUpdater>();
    readonly IEventSensor<GaugeEvent> queueLength;
    readonly QueueBindings bindings;
    readonly List<Tuple<string, MessageQueue>> counters = new List<Tuple<string, MessageQueue>>();


    public MsmqQueueLengthUpdater(IEventSensor<GaugeEvent> queueLength, QueueBindings bindings)
    {
        this.queueLength = queueLength;
        this.bindings = bindings;
    }

    public void Warmup()
    {
        counters.AddRange(
            from q in bindings.ReceivingAddresses
            let split = q.Split('@')
            let queueName = split[0]
            let path = $@".\private$\{queueName}"
            select Tuple.Create(
                q,
                new MessageQueue(path, QueueAccessMode.Peek)
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

    static long TryGet(MessageQueue counter, long defaultValue)
    {
        try
        {
            return counter.GetCount();
        }
        catch (Exception ex)
        {
            Log.Info("TryGet", ex);
            return defaultValue;
        }
    }
}
