namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_publishing_message : NServiceBusAcceptanceTest
    {
        static Guid HostId = Guid.NewGuid();

        [Test]
        public async Task Should_enhance_it_with_queue_length_properties()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(c => c.When(async s =>
                {
                    await s.Publish(new TestEventMessage1());
                    await s.Publish(new TestEventMessage1());
                    await s.Publish(new TestEventMessage2());
                    await s.Publish(new TestEventMessage2());
                }))
                .WithEndpoint<Subscriber>()
                .Done(c => c.Headers1.Count == 2 && c.Headers2.Count == 2)
                .Run()
                .ConfigureAwait(false);

            //var sessionIds = new [] { AssertHeaders(context.Headers1), AssertHeaders(context.Headers2)};

            //// assert data
            //var data = JObject.Parse(context.Data);
            //var counters = (JArray)data["Counters"];
            //var counterTokens = counters.Where(c => c.Value<string>("Name").StartsWith("QueueLengthSend_"));

            //foreach (var counter in counterTokens)
            //{
            //    var name = counter.Value<string>("Name");
            //    var counterNameBasedSessionId = Guid.Parse(name.Substring(name.LastIndexOf("_") + 1));

            //    CollectionAssert.Contains(sessionIds, counterNameBasedSessionId);
            //    Assert.AreEqual(2, counter.Value<int>("Count"));
            //}
        }

        static Guid AssertHeaders(IProducerConsumerCollection<IReadOnlyDictionary<string, string>> oneReceiverHeaders)
        {
            var headers = oneReceiverHeaders.ToArray();

            Guid sessionId1, sessionId2;
            long sequence1, sequence2;

            Parse(headers[0], out sessionId1, out sequence1);
            Parse(headers[1], out sessionId2, out sequence2);

            Assert.AreEqual(sessionId1, sessionId2);
            Assert.AreEqual(1, sequence1);
            Assert.AreEqual(2, sequence2);
            return sessionId1;
        }

        static void Parse(IReadOnlyDictionary<string, string> headers, out Guid sessionId, out long sequence)
        {
            var rawHeader = headers["NServiceBus.Metrics.QueueLength"];
            var parts = rawHeader.Split('_');
            sessionId = Guid.Parse(parts[0]);
            sequence = long.Parse(parts[1]);
        }

        class Context : ScenarioContext
        {
            public ConcurrentQueue<IReadOnlyDictionary<string, string>> Headers1 { get; } = new ConcurrentQueue<IReadOnlyDictionary<string, string>>();
            public ConcurrentQueue<IReadOnlyDictionary<string, string>> Headers2 { get; } = new ConcurrentQueue<IReadOnlyDictionary<string, string>>();
            public string Data { get; set; }
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    var context = (Context)r.ScenarioContext;

                    c.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(HostId);
                    c.EnableMetrics().EnableCustomReport(payload =>
                    {
                        context.Data = payload;
                        return Task.FromResult(0);
                    }, TimeSpan.FromMilliseconds(5));
                });
            }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));
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
    }
}