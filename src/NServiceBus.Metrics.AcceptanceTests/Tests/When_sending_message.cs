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

    public class When_sending_message : NServiceBusAcceptanceTest
    {
        static string ReceiverAddress1 => Conventions.EndpointNamingConvention(typeof(Receiver1));
        static string ReceiverAddress2 => Conventions.EndpointNamingConvention(typeof(Receiver2));

        static Guid HostId = Guid.NewGuid();

        [Test]
        public async Task Should_enhance_it_with_queue_length_properties()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(c => c.When(async s =>
                {
                    var to1 = new SendOptions();
                    to1.SetDestination(ReceiverAddress1);
                    await s.Send(new TestMessage(), to1);
                    await s.Send(new TestMessage(), to1);

                    var to2 = new SendOptions();
                    to2.SetDestination(ReceiverAddress2);
                    await s.Send(new TestMessage(), to2);
                    await s.Send(new TestMessage(), to2);

                }))
                .WithEndpoint<Receiver1>()
                .WithEndpoint<Receiver2>()
                .Done(c => c.Headers1.Count == 2 && c.Headers2.Count == 2)
                .Run()
                .ConfigureAwait(false);

            var sessionIds = new [] { AssertHeaders(context.Headers1), AssertHeaders(context.Headers2)};

            var data = JObject.Parse(context.Data);
            var counters = (JArray)data["Counters"];
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
            var headers = oneReceiverHeaders.ToArray();

            var sessionKey1 = headers[0]["NServiceBus.Metrics.QueueLength.Key"];
            var sessionKey2 = headers[1]["NServiceBus.Metrics.QueueLength.Key"];

            var sequence1 = long.Parse(headers[0]["NServiceBus.Metrics.QueueLength.Value"]);
            var sequence2 = long.Parse(headers[1]["NServiceBus.Metrics.QueueLength.Value"]);

            Assert.AreEqual(sessionKey1, sessionKey2);
            Assert.AreEqual(1, sequence1);
            Assert.AreEqual(2, sequence2);
            
            return sessionKey1;
        }

        class Context : ScenarioContext
        {
            public ConcurrentQueue<IReadOnlyDictionary<string, string>> Headers1 { get; } = new ConcurrentQueue<IReadOnlyDictionary<string, string>>();
            public ConcurrentQueue<IReadOnlyDictionary<string, string>> Headers2 { get; } = new ConcurrentQueue<IReadOnlyDictionary<string, string>>();
            public string Data { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
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

        class Receiver1 : EndpointConfigurationBuilder
        {
            public Receiver1()
            {
                EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));
            }

            public class TestMessageHandler : IHandleMessages<TestMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(TestMessage message, IMessageHandlerContext context)
                {
                    TestContext.Headers1.Enqueue(context.MessageHeaders);

                    return Task.FromResult(0);
                }
            }
        }

        class Receiver2 : EndpointConfigurationBuilder
        {
            public Receiver2()
            {
                EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));
            }

            public class TestMessageHandler : IHandleMessages<TestMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(TestMessage message, IMessageHandlerContext context)
                {
                    TestContext.Headers2.Enqueue(context.MessageHeaders);

                    return Task.FromResult(0);
                }
            }
        }

        public class TestMessage : ICommand
        {
        }
    }
}