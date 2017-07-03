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

    class QueueLengthTracker
    {
        const string KeyHeaderName = "NServiceBus.Metrics.QueueLength.Key";
        const string ValueHeaderName = "NServiceBus.Metrics.QueueLength.Value";

        MetricsContext metricsContext;

        ConcurrentDictionary<string, CounterImplementation> sendingCounters = new ConcurrentDictionary<string, CounterImplementation>();
        ConcurrentDictionary<string, SequenceReporter> receivingReporters = new ConcurrentDictionary<string, SequenceReporter>();
        static readonly Unit Unit = Unit.Custom("Sequence");

        private QueueLengthTracker(MetricsContext metricsContext)
        {
            this.metricsContext = metricsContext;
        }

        public static void SetUp(MetricsContext metricsContext, FeatureConfigurationContext featureContext)
        {
            var queueLengthTracker = new QueueLengthTracker(metricsContext);

            var pipeline = featureContext.Pipeline;

            //Use HostId as a stable session ID
            pipeline.Register(b => new DispatchQueueLengthBehavior(queueLengthTracker, b.Build<HostInformation>().HostId), nameof(DispatchQueueLengthBehavior));

            pipeline.Register(new IncomingQueueLengthBehavior(queueLengthTracker, featureContext.Settings.LocalAddress()), nameof(IncomingQueueLengthBehavior));
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
            readonly QueueLengthTracker queueLengthTracker;
            readonly Guid session;

            public DispatchQueueLengthBehavior(QueueLengthTracker queueLengthTracker, Guid session)
            {
                this.queueLengthTracker = queueLengthTracker;
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
                        sequence = queueLengthTracker.RegisterSend(key);
                    }
                    else
                    {
                        var multicastAddressTag = transportOperation.AddressTag as MulticastAddressTag;
                        if (multicastAddressTag != null)
                        {
                            key = BuildKey(multicastAddressTag.MessageType.AssemblyQualifiedName);
                            sequence = queueLengthTracker.RegisterSend(key);
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
            readonly QueueLengthTracker queueLengthTracker;
            readonly string inputQueue;

            public IncomingQueueLengthBehavior(QueueLengthTracker queueLengthTracker, string inputQueue)
            {
                this.queueLengthTracker = queueLengthTracker;
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
                        queueLengthTracker.RegisterReceive(key, sequence, inputQueue);
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
