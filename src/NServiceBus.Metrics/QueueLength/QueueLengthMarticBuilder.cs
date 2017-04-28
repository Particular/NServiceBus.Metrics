namespace NServiceBus.Metrics.QueueLength
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Features;
    using global::Metrics;
    using global::Metrics.Core;
    using Pipeline;
    using Routing;

    class QueueLengthMetricBuilder : MetricBuilder
    {
        MetricsContext metricsContext;

        ConcurrentDictionary<string, Tuple<Guid, CounterImplementation>> sendingCounters = new ConcurrentDictionary<string, Tuple<Guid, CounterImplementation>>();
        
        public override void Define(MetricsContext metricsContext)
        {
            this.metricsContext = metricsContext;          
        }

        public override void WireUp(FeatureConfigurationContext featureConfigurationContext)
        {
            var queuLengthSendBehavior = new HookupBehavior(this);

            featureConfigurationContext.Pipeline.Register(queuLengthSendBehavior, "QueuLengthSendBehavior");
        }

        Tuple<Guid, long> RegisterSend(string destination)
        {
            var counter = sendingCounters.GetOrAdd(destination, CreateSendCounter);
            
            counter.Item2.Increment();

            return Tuple.Create(counter.Item1, counter.Item2.Value.Count);
        }

        Tuple<Guid, CounterImplementation> CreateSendCounter(string destination)
        {
            var sessionId = Guid.NewGuid();

            var counter = (CounterImplementation) metricsContext.Counter("QueueLengthSend_" + destination + "_" + sessionId, Unit.Custom("Sequence"));

            return Tuple.Create(sessionId, counter);
        }

        class HookupBehavior : IBehavior<IDispatchContext, IDispatchContext>
        {
            readonly QueueLengthMetricBuilder queueLengthMetricBuilder;

            public HookupBehavior(QueueLengthMetricBuilder queueLengthMetricBuilder)
            {
                this.queueLengthMetricBuilder = queueLengthMetricBuilder;
            }

            public Task Invoke(IDispatchContext context, Func<IDispatchContext, Task> next)
            {
                foreach (var transportOperation in context.Operations)
                {
                    var unicastAddressTag = transportOperation.AddressTag as UnicastAddressTag;

                    Tuple<Guid, long> sessionIdWithCounter = null;

                    if (unicastAddressTag != null)
                    {
                        sessionIdWithCounter = queueLengthMetricBuilder.RegisterSend(unicastAddressTag.Destination);
                    }
                    else
                    {
                        var multicastAddressTag = transportOperation.AddressTag as MulticastAddressTag;

                        if (multicastAddressTag != null)
                        {
                            sessionIdWithCounter = queueLengthMetricBuilder.RegisterSend(multicastAddressTag.MessageType.FullName);
                        }
                    }

                    if (sessionIdWithCounter != null)
                    {
                        transportOperation.Message.Headers["NServiceBus.Metrics.QueueLength"] = sessionIdWithCounter.Item1 + "_" + sessionIdWithCounter.Item2;
                    }
                }

                return next(context);
            }
        }
    }
}
