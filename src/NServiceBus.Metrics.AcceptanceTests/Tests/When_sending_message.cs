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
        static string ReceiverAddress => Conventions.EndpointNamingConvention(typeof(Receiver));
        static Guid HostId = Guid.NewGuid();

        [Test]
        public async Task Should_enhance_it_with_queue_length_properties()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(c => c.When(async session =>
                {
                    var sendOptions = new SendOptions();
                    sendOptions.SetDestination(ReceiverAddress);

                    await session.Send(new TestMessage(), sendOptions);
                    await session.Send(new TestMessage(), sendOptions);
                }))
                .WithEndpoint<Receiver>()
                .Done(c => c.Headers.Count == 2)
                .Run()
                .ConfigureAwait(false);

            var headers = context.Headers.ToArray();

            Guid sessionId1, sessionId2;
            long sequence1, sequence2;

            Parse(headers[0], out sessionId1, out sequence1);
            Parse(headers[1], out sessionId2, out sequence2);

            Assert.AreEqual(sessionId1, sessionId2);
            Assert.AreEqual(1, sequence1);
            Assert.AreEqual(2, sequence2);

            // assert data
            var data = JObject.Parse(context.Data);
            var counters = (JArray)data["Counters"];
            var counter = counters.Single(c => c.Value<string>("Name").StartsWith("QueueLengthSend_"));

            var name = counter.Value<string>("Name");
            var counterNameBasedSessionId = Guid.Parse(name.Substring(name.LastIndexOf("_") + 1));

            Assert.AreEqual(sessionId1, counterNameBasedSessionId);
            Assert.AreEqual(2, counter.Value<int>("Count"));
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
            public ConcurrentQueue<IReadOnlyDictionary<string, string>> Headers { get; set; } = new ConcurrentQueue<IReadOnlyDictionary<string, string>>();
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

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c => c.LimitMessageProcessingConcurrencyTo(1));
            }

            public class TestMessageHandler : IHandleMessages<TestMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(TestMessage message, IMessageHandlerContext context)
                {
                    TestContext.Headers.Enqueue(context.MessageHeaders);

                    return Task.FromResult(0);
                }
            }
        }

        public class TestMessage : ICommand
        {
        }
    }
}