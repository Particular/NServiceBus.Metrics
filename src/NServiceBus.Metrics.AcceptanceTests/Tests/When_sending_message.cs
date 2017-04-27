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

            // assert data
            var data = JObject.Parse(context.Data);
            var counters = (JArray)data["Counters"];
            var counterTokens = counters.Where(c => c.Value<string>("Name").StartsWith("QueueLengthSend_"));

            foreach (var counter in counterTokens)
            {
                var name = counter.Value<string>("Name");
                var counterNameBasedSessionId = Guid.Parse(name.Substring(name.LastIndexOf("_") + 1));

                CollectionAssert.Contains(sessionIds, counterNameBasedSessionId);
                Assert.AreEqual(2, counter.Value<int>("Count"));
            }
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