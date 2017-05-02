namespace NServiceBus.Metrics.QueueLength
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using global::Metrics;
    using global::Metrics.Core;
    using Pipeline;
    using Routing;

    class QueueLengthMetricBuilder : MetricBuilder
    {
        const string HeaderName = "NServiceBus.Metrics.QueueLength";

        const char SeparatorValue = '_';
        static readonly string Separator = SeparatorValue.ToString();
        static readonly char[] Separators = { SeparatorValue };

        MetricsContext metricsContext;

        ConcurrentDictionary<string, Tuple<Guid, CounterImplementation>> sendingCounters = new ConcurrentDictionary<string, Tuple<Guid, CounterImplementation>>();
        ConcurrentDictionary<Guid, SequenceReporter> receivingReporters = new ConcurrentDictionary<Guid, SequenceReporter>();
        static readonly Unit Unit = Unit.Custom("Sequence");

        public override void Define(MetricsContext metricsContext)
        {
            this.metricsContext = metricsContext;
        }

        public override void WireUp(FeatureConfigurationContext featureConfigurationContext)
        {
            var pipeline = featureConfigurationContext.Pipeline;

            var dispatchBehavior = new DispatchQueueLengthBehavior(this);
            pipeline.Register(dispatchBehavior, nameof(DispatchQueueLengthBehavior));

            var incomingBehavior = new IncomingQueueLengthBehavior(this);
            pipeline.Register(incomingBehavior, nameof(IncomingQueueLengthBehavior));
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
            var counter = (CounterImplementation)metricsContext.Counter("QueueLengthSend" + Separator + destination + Separator + sessionId, Unit);
            return Tuple.Create(sessionId, counter);
        }

        void RegisterReceive(Guid sessionId, long sequence)
        {
            var reporter = receivingReporters.GetOrAdd(sessionId, CreateGauge);
            reporter.Report(sequence);
        }

        SequenceReporter CreateGauge(Guid sessionId)
        {
            var reporter = new SequenceReporter();
            metricsContext.Gauge("QueueLengthReceive" + Separator + sessionId, reporter.GetValue, Unit);
            return reporter;
        }

        class DispatchQueueLengthBehavior : IBehavior<IDispatchContext, IDispatchContext>
        {
            readonly QueueLengthMetricBuilder queueLengthMetricBuilder;

            public DispatchQueueLengthBehavior(QueueLengthMetricBuilder queueLengthMetricBuilder)
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
                        transportOperation.Message.Headers[HeaderName] = sessionIdWithCounter.Item1 + Separator + sessionIdWithCounter.Item2;
                    }
                }

                return next(context);
            }
        }

        class IncomingQueueLengthBehavior : IBehavior<IIncomingLogicalMessageContext, IIncomingLogicalMessageContext>
        {
            readonly QueueLengthMetricBuilder queueLengthMetricBuilder;

            public IncomingQueueLengthBehavior(QueueLengthMetricBuilder queueLengthMetricBuilder)
            {
                this.queueLengthMetricBuilder = queueLengthMetricBuilder;
            }

            public Task Invoke(IIncomingLogicalMessageContext context, Func<IIncomingLogicalMessageContext, Task> next)
            {
                string value;
                if (context.Headers.TryGetValue(HeaderName, out value))
                {
                    var parts = value.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                    Guid sessionId;
                    long sequence;

                    if (parts.Length == 2 &&
                        Guid.TryParse(parts[0], out sessionId) &&
                        long.TryParse(parts[1], out sequence))
                    {
                        queueLengthMetricBuilder.RegisterReceive(sessionId, sequence);
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
