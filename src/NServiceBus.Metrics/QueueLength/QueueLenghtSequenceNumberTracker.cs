namespace NServiceBus.Metrics.QueueLength
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using global::Metrics;
    using global::Metrics.Core;
    using Hosting;
    using Pipeline;
    using Routing;

    class QueueLenghtSequenceNumberTracker
    {
        const string KeyHeaderName = "NServiceBus.Metrics.QueueLength.Key";
        const string ValueHeaderName = "NServiceBus.Metrics.QueueLength.Value";

        MetricsContext metricsContext;

        ConcurrentDictionary<string, CounterImplementation> sendingCounters = new ConcurrentDictionary<string, CounterImplementation>();
        ConcurrentDictionary<string, SequenceReporter> receivingReporters = new ConcurrentDictionary<string, SequenceReporter>();
        static readonly Unit Unit = Unit.Custom("Sequence");


        public void WireUp(FeatureConfigurationContext featureConfigurationContext)
        {
            var pipeline = featureConfigurationContext.Pipeline;

            //Use HostId as a stable session ID
            pipeline.Register(b => new DispatchQueueLengthBehavior(this, b.Build<HostInformation>().HostId), nameof(DispatchQueueLengthBehavior));

            var incomingBehavior = new IncomingQueueLengthBehavior(this, featureConfigurationContext.Settings.LocalAddress());
            pipeline.Register(incomingBehavior, nameof(IncomingQueueLengthBehavior));
        }

        public void AttachMetricsContext(MetricsContext context)
        {
            metricsContext = context;
        }

        long RegisterSend(string key)
        {
            var counter = sendingCounters.GetOrAdd(key, CreateSendCounter);
            counter.Increment();

            return counter.Value.Count;
        }

        CounterImplementation CreateSendCounter(string key)
        {
            var tags = new MetricTags($"key:{key}", "type:queue-length.sent");
            var counter = (CounterImplementation)metricsContext.Counter("Sent sequence for " + key, Unit, tags);
            return counter;
        }

        void RegisterReceive(string key, long sequence, string inputQueue)
        {
            var reporter = receivingReporters.GetOrAdd(key, k => CreateGauge(k, inputQueue));
            reporter.Report(sequence);
        }

        SequenceReporter CreateGauge(string key, string inputQueue)
        {
            var reporter = new SequenceReporter();
            var tags = new MetricTags($"key:{key}", $"queue:{inputQueue}", "type:queue-length.received");
            metricsContext.Gauge("Received sequence for " + key, reporter.GetValue, Unit, tags);
            return reporter;
        }

        class DispatchQueueLengthBehavior : IBehavior<IDispatchContext, IDispatchContext>
        {
            readonly QueueLenghtSequenceNumberTracker queueLenghtSequenceNumberTracker;
            readonly Guid session;

            public DispatchQueueLengthBehavior(QueueLenghtSequenceNumberTracker queueLenghtSequenceNumberTracker, Guid session)
            {
                this.queueLenghtSequenceNumberTracker = queueLenghtSequenceNumberTracker;
                this.session = session;
            }

            public Task Invoke(IDispatchContext context, Func<IDispatchContext, Task> next)
            {
                foreach (var transportOperation in context.Operations)
                {
                    var unicastAddressTag = transportOperation.AddressTag as UnicastAddressTag;

                    long sequence;
                    string key;

                    if (unicastAddressTag != null)
                    {
                        key = BuildKey(unicastAddressTag.Destination);
                        sequence = queueLenghtSequenceNumberTracker.RegisterSend(key);
                    }
                    else
                    {
                        var multicastAddressTag = transportOperation.AddressTag as MulticastAddressTag;
                        if (multicastAddressTag != null)
                        {
                            key = BuildKey(multicastAddressTag.MessageType.AssemblyQualifiedName);
                            sequence = queueLenghtSequenceNumberTracker.RegisterSend(key);
                        }
                        else
                        {
                            throw new Exception("Not supported address tag");
                        }
                    }
                    transportOperation.Message.Headers[KeyHeaderName] = key;
                    transportOperation.Message.Headers[ValueHeaderName] = sequence.ToString();
                }
                return next(context);
            }

            string BuildKey(string destination)
            {
                return $"{destination}-{session}".ToLowerInvariant();
            }
        }

        class IncomingQueueLengthBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
        {
            readonly QueueLenghtSequenceNumberTracker queueLenghtSequenceNumberTracker;
            readonly string inputQueue;

            public IncomingQueueLengthBehavior(QueueLenghtSequenceNumberTracker queueLenghtSequenceNumberTracker, string inputQueue)
            {
                this.queueLenghtSequenceNumberTracker = queueLenghtSequenceNumberTracker;
                this.inputQueue = inputQueue;
            }

            public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
            {
                string key, value;
                if (context.Headers.TryGetValue(KeyHeaderName, out key) && context.Headers.TryGetValue(ValueHeaderName, out value))
                {
                    long sequence;

                    if (long.TryParse(value, out sequence))
                    {
                        queueLenghtSequenceNumberTracker.RegisterReceive(key, sequence, inputQueue);
                    }
                }
                return next(context);
            }
        }

        class SequenceReporter
        {
            long value;

            public double GetValue()
            {
                return Volatile.Read(ref value);
            }

            public void Report(long v)
            {
                Volatile.Write(ref value, v);
            }
        }
    }
}
