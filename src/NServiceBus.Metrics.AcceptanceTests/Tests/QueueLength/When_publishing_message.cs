using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Extensibility;
using NServiceBus.Features;
using NServiceBus.Metrics;
using NServiceBus.Metrics.AcceptanceTests;
using NServiceBus.ObjectBuilder;
using NServiceBus.Pipeline;
using NServiceBus.Routing;
using NUnit.Framework;
using NServiceBus.Transport;

public class When_publishing_message : NServiceBusAcceptanceTest
{
    static Guid HostId = Guid.NewGuid();

    [Test]
    public async Task Should_enhance_it_with_queue_length_properties()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Subscriber>(b => b.When(ctx => ctx.EndpointsStarted, async (session, c) =>
            {
                await session.Subscribe<TestEventMessage1>();
                await session.Subscribe<TestEventMessage2>();
            }))
            .WithEndpoint<Publisher>(c => c.When(ctx => ctx.SubscriptionCount == 2, async s =>
            {
                await s.Publish(new TestEventMessage1());
                await s.Publish(new TestEventMessage1());
                await s.Publish(new TestEventMessage2());
                await s.Publish(new TestEventMessage2());
            }))
            .WithEndpoint<MonitoringSpy>()
            .Done(c => c.Headers1.Count == 2 && c.Headers2.Count == 2 && c.Data != null)
            .Run()
            .ConfigureAwait(false);

        AssertSequencesReported(context);
    }

    static void AssertSequencesReported(Context context)
    {
        var sessionIds = new[]
        {
            AssertHeaders(context.Headers1),
            AssertHeaders(context.Headers2)
        };

        var data = JObject.Parse(context.Data);
        var counters = (JArray) data["Counters"];
        var counterTokens = counters.Where(c => c.Value<string>("Name").StartsWith("Sent sequence for"));

        foreach (var counter in counterTokens)
        {
            var tags = counter["Tags"].ToObject<string[]>();
            var counterBasedKey = tags.GetTagValue("key");
            var type = tags.GetTagValue("type");

            CollectionAssert.Contains(sessionIds, counterBasedKey);
            Assert.AreEqual(2, counter.Value<int>("Count"));
            Assert.AreEqual("queue-length.sent", type);
        }
    }

    static string AssertHeaders(IProducerConsumerCollection<IReadOnlyDictionary<string, string>> oneReceiverHeaders)
    {
        const string keyHeader = "NServiceBus.Metrics.QueueLength.Key";
        const string valueHeader = "NServiceBus.Metrics.QueueLength.Value";

        var headers = oneReceiverHeaders.ToArray();

        var sessionKey1 = headers[0][keyHeader];
        var sessionKey2 = headers[1][keyHeader];
        Assert.AreEqual(sessionKey1, sessionKey2, "expected sessionKey1 == sessionKey2");

        var sequence1 = long.Parse(headers[0][valueHeader]);
        var sequence2 = long.Parse(headers[1][valueHeader]);
        Assert.AreNotEqual(sequence2, sequence1, "expected sequence1 != sequence2");
        Assert.IsTrue(sequence1 == 1 || sequence2 == 1, "sequence1 == 1 || sequence2 == 1");
        Assert.IsTrue(sequence1 == 2 || sequence2 == 2, "sequence1 == 2 || sequence2 == 2");
        return sessionKey1;
    }

    class Context : ScenarioContext
    {
        public volatile int SubscriptionCount;

        public ConcurrentQueue<IReadOnlyDictionary<string, string>> Headers1 { get; } = new ConcurrentQueue<IReadOnlyDictionary<string, string>>();
        public ConcurrentQueue<IReadOnlyDictionary<string, string>> Headers2 { get; } = new ConcurrentQueue<IReadOnlyDictionary<string, string>>();

        public string Data { get; set; }

        public Func<bool> TrackReports;
        public Context()
        {
            TrackReports = () => Headers1.Count == 2 && Headers2.Count == 2;
        }
    }

    class Publisher : EndpointConfigurationBuilder
    {
        public Publisher()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(HostId);

                c.OnEndpointSubscribed<Context>((s, ctx) =>
                {
                    if (s.SubscriberReturnAddress.Contains("Subscriber"))
                    {
                        Interlocked.Increment(ref ctx.SubscriptionCount);
                    }
                });

                c.Pipeline.Register(new PreQueueLengthStep());
                c.Pipeline.Register(new PostQueueLengthStep());

#pragma warning disable 618
                var address = NServiceBus.AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(MonitoringSpy));
                var metrics = c.EnableMetrics();
                metrics.SendMetricDataToServiceControl(address, TimeSpan.FromSeconds(5));
#pragma warning restore 618
            });
        }
    }

    class Subscriber : EndpointConfigurationBuilder
    {
        public Subscriber()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.LimitMessageProcessingConcurrencyTo(1);
                c.DisableFeature<AutoSubscribe>();

                var routing = c.UseTransport<MsmqTransport>().Routing();
                var publisher = NServiceBus.AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Publisher));
                routing.RegisterPublisher(typeof(TestEventMessage1), publisher);
                routing.RegisterPublisher(typeof(TestEventMessage2), publisher);
            });
        }

        public class TestEventMessage1Handler : IHandleMessages<TestEventMessage1>
        {
            public Context TestContext { get; set; }

            public Task Handle(TestEventMessage1 message, IMessageHandlerContext context)
            {
                TestContext.Headers1.Enqueue(context.MessageHeaders);

                return Task.FromResult(0);
            }
        }

        public class TestEventMessage2Handler : IHandleMessages<TestEventMessage2>
        {
            public Context TestContext { get; set; }

            public Task Handle(TestEventMessage2 message, IMessageHandlerContext context)
            {
                TestContext.Headers2.Enqueue(context.MessageHeaders);
                return Task.FromResult(0);
            }
        }
    }

    protected class MonitoringSpy : EndpointConfigurationBuilder
    {
        public MonitoringSpy()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.UseSerialization<NewtonsoftSerializer>();
                c.LimitMessageProcessingConcurrencyTo(1);
            }).IncludeType<MetricReport>();
        }

        class MetricHandler : IHandleMessages<MetricReport>
        {
            public Context TestContext { get; set; }

            public Task Handle(MetricReport message, IMessageHandlerContext context)
            {
                if (TestContext.TrackReports())
                {
                    TestContext.Data = message.Data.ToString();
                }

                return Task.FromResult(0);
            }
        }
    }

    public class TestEventMessage1 : IEvent
    {
    }

    public class TestEventMessage2 : IEvent
    {
    }

    class PreQueueLengthStep : RegisterStep
    {
        public PreQueueLengthStep()
            : base("PreQueueLengthStep", typeof(Behavior), "Registers behavior replacing context")
        {
            InsertBefore("DispatchQueueLengthBehavior");
        }

        class Behavior : IBehavior<IDispatchContext,IDispatchContext>
        {
            public Task Invoke(IDispatchContext context, Func<IDispatchContext, Task> next)
            {
                return next(new MultiDispatchContext(context));
            }
        }
    }

    class PostQueueLengthStep : RegisterStep
    {
        public PostQueueLengthStep()
            : base("PostQueueLengthStep", typeof(Behavior), "Registers behavior restoring context")
        {
            InsertAfter("DispatchQueueLengthBehavior");
        }

        class Behavior : IBehavior<IDispatchContext, IDispatchContext>
        {
            public Task Invoke(IDispatchContext context, Func<IDispatchContext, Task> next)
            {
                return next(((MultiDispatchContext) context).Original);
            }
        }
    }

    class MultiDispatchContext : IDispatchContext
    {
        public MultiDispatchContext(IDispatchContext original)
        {
            Extensions = original.Extensions;
            Builder = original.Builder;
            Operations = original.Operations.Select(t => new TransportOperation(t.Message, new MulticastAddressTag(Type.GetType(t.Message.Headers[Headers.EnclosedMessageTypes])), t.RequiredDispatchConsistency, t.DeliveryConstraints)).ToArray();
            Original = original;
        }

        public IDispatchContext Original { get; }
        public ContextBag Extensions { get; }
        public IBuilder Builder { get; }
        public IEnumerable<TransportOperation> Operations { get; }
    }
}
