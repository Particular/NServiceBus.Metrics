namespace NServiceBus.Metrics.QueueLength
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Features;
    using Hosting;
    using Pipeline;
    using Routing;

    class QueueLengthTracker
    {
        const string KeyHeaderName = "NServiceBus.Metrics.QueueLength.Key";
        const string ValueHeaderName = "NServiceBus.Metrics.QueueLength.Value";

        Context metricsContext;

        ConcurrentDictionary<string, Counter> sendingCounters = new ConcurrentDictionary<string, Counter>();
        ConcurrentDictionary<string, Gauge> receivingReporters = new ConcurrentDictionary<string, Gauge>();

        QueueLengthTracker(Context metricsContext)
        {
            this.metricsContext = metricsContext;
        }

        public static void SetUp(Context metricsContext, FeatureConfigurationContext featureContext)
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
            return counter.Increment();
        }

        Counter CreateSendCounter(string key)
        {
            return metricsContext.Counter(key);
        }

        void RegisterReceive(string key, long sequence, string inputQueue)
        {
            var reporter = receivingReporters.GetOrAdd(key, k => CreateGauge(k, inputQueue));
            reporter.Report(sequence);
        }

        Gauge CreateGauge(string key, string inputQueue)
        {
            return metricsContext.Gauge(key, inputQueue);
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
                if (context.Headers.TryGetValue(KeyHeaderName, out var key) && context.Headers.TryGetValue(ValueHeaderName, out var value))
                {
                    if (long.TryParse(value, out var sequence))
                    {
                        queueLengthTracker.RegisterReceive(key, sequence, inputQueue);
                    }
                }
                return next(context);
            }
        }
    }
}
