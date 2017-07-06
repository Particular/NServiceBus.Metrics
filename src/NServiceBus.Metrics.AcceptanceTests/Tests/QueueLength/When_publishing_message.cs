namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Extensibility;
    using Features;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ObjectBuilder;
    using Pipeline;
    using Routing;
    using Transport;

    public class When_publishing_message : QueueLengthAcceptanceTests
    {
        static Guid HostId = Guid.NewGuid();

        [Test]
        public async Task Should_enhance_it_with_queue_length_properties()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(c => c.When(ctx => ctx.SubscriptionCount == 2, async s =>
                  {
                      await s.Publish(new TestEventMessage1());
                      await s.Publish(new TestEventMessage1());
                      await s.Publish(new TestEventMessage2());
                      await s.Publish(new TestEventMessage2());
                  }))
                .WithEndpoint<Subscriber>(b => b.When(async (session, c) =>
                {
                    await session.Subscribe<TestEventMessage1>();
                    await session.Subscribe<TestEventMessage2>();
                }))
                .WithEndpoint<MonitoringSpy>()
                .Done(c => c.Headers1.Count == 2 && c.Headers2.Count == 2 && c.Data != null)
                .Run()
                .ConfigureAwait(false);

            SequencesReported(context);
        }

        static void SequencesReported(Context context)
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

            var sequence1 = long.Parse(headers[0][valueHeader]);
            var sequence2 = long.Parse(headers[1][valueHeader]);

            Assert.AreEqual(sessionKey1, sessionKey2);
            Assert.AreEqual(1, sequence1);
            Assert.AreEqual(2, sequence2);

            return sessionKey1;
        }

        class Context : QueueLengthContext
        {
            public volatile int SubscriptionCount;
            public ConcurrentQueue<IReadOnlyDictionary<string, string>> Headers1 { get; } = new ConcurrentQueue<IReadOnlyDictionary<string, string>>();
            public ConcurrentQueue<IReadOnlyDictionary<string, string>> Headers2 { get; } = new ConcurrentQueue<IReadOnlyDictionary<string, string>>();

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
                    c.EnableMetrics().SendMetricDataToServiceControl(MonitoringSpyAddress, TimeSpan.FromSeconds(5));
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

                    var routing = c.UseTransport<MsmqTransport>()
                        .Routing();
                    var publisher = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Publisher));
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
}