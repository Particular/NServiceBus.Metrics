namespace NServiceBus.Metrics.QueueLength
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Features;
    using global::Metrics;
    using Pipeline;
    using Routing;

    class QueueLengthMetricBuilder : MetricBuilder
    {
        MetricsContext metricsContext;

        ConcurrentDictionary<string, Counter> sendingCounters = new ConcurrentDictionary<string, Counter>();
        
        public override void Define(MetricsContext metricsContext)
        {
            this.metricsContext = metricsContext;          
        }

        public override void WireUp(FeatureConfigurationContext featureConfigurationContext)
        {
            var queuLengthSendBehavior = new HookupBehavior(this);

            featureConfigurationContext.Pipeline.Register(queuLengthSendBehavior, "QueuLengthSendBehavior");
        }

        void RegisterSend(string destination)
        {
            // wire up the pipline and register sends
            var counter = sendingCounters.GetOrAdd(destination, CreateSendCounter);

            counter.Increment();
        }

        Counter CreateSendCounter(string destination)
        {
            return metricsContext.Counter("QueueLengthSend_" + destination + "_" + Guid.NewGuid(), Unit.Custom("Sequence"));
        }

        class HookupBehavior : IBehavior<IDispatchContext, IDispatchContext>
        {
            readonly QueueLengthMetricBuilder queueLengthMetricBuilder;

            static readonly Task compledTask = Task.FromResult(0);

            public HookupBehavior(QueueLengthMetricBuilder queueLengthMetricBuilder)
            {
                this.queueLengthMetricBuilder = queueLengthMetricBuilder;
            }

            public Task Invoke(IDispatchContext context, Func<IDispatchContext, Task> next)
            {
                foreach (var transportOperation in context.Operations)
                {
                    var unicastAddressTag = transportOperation.AddressTag as UnicastAddressTag;

                    if (unicastAddressTag != null)
                    {
                        queueLengthMetricBuilder.RegisterSend(unicastAddressTag.Destination);
                    }
                }

                return compledTask;
            }
        }
    }
}
