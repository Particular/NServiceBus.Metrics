namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_message : NServiceBusAcceptanceTest
    {
        const string Header = "NServiceBus.Metrics.QueueLength";
        const int SequenceValue = 42;
        static readonly string sessionId = Guid.NewGuid().ToString();

        [Test]
        public async Task Should_report_sequence_for_session()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>(c => c.When(async s =>
                {
                    var options = new SendOptions();
                    options.RouteToThisEndpoint();

                    options.SetHeader(Header, sessionId + "_" + SequenceValue);
                    await s.Send(new TestMessage(), options);
                }))
                .Done(c => c.Handled && c.Data != null)
                .Run()
                .ConfigureAwait(false);

            // assert data
            var data = JObject.Parse(context.Data);
            var gauges = (JArray)data["Gauges"];
            var gauge = gauges.Single(c => c.Value<string>("Name").StartsWith("QueueLengthReceive_"));

            var name = gauge.Value<string>("Name");
            Assert.True(name.EndsWith(sessionId), $"Gauge name should end with '{sessionId}' but was '{name}'");
            Assert.AreEqual(SequenceValue.ToString(), gauge.Value<string>("Value"));
        }

        class Context : ScenarioContext
        {
            public string Data { get; set; }
            public bool Handled { get; set; }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>((c,r) =>
                {
                    var context = (Context)r.ScenarioContext;

                    c.LimitMessageProcessingConcurrencyTo(1);
                    c.EnableMetrics().EnableCustomReport(payload =>
                    {
                        context.Data = payload;
                        return Task.FromResult(0);
                    }, TimeSpan.FromMilliseconds(5));

                    c.Pipeline.Remove("DispatchQueueLengthBehavior");
                });
            }

            public class TestMessageHandler : IHandleMessages<TestMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(TestMessage message, IMessageHandlerContext context)
                {
                    TestContext.Handled = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class TestMessage : ICommand
        {
        }
    }
}