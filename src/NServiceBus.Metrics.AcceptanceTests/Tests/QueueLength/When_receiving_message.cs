namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_receiving_message : QueueLengthAcceptanceTests
    {

        const string KeyHeader = "NServiceBus.Metrics.QueueLength.Key";
        const string ValueHeader = "NServiceBus.Metrics.QueueLength.Value";
        const string SequenceValue = "42";
        static readonly string SequenceKey = Guid.NewGuid().ToString();

        [Test]
        public async Task Should_report_sequence_for_session()
        {
            var context = await Scenario.Define<QueueLengthContext>()
                .WithEndpoint<Receiver>(c => c.When(async s =>
                {
                    var options = new SendOptions();
                    options.RouteToThisEndpoint();

                    options.SetHeader(KeyHeader, SequenceKey);
                    options.SetHeader(ValueHeader, SequenceValue);
                    await s.Send(new TestMessage(), options);
                }))
                .WithEndpoint<MonitoringSpy>()
                .Done(c => c.Data != null && c.Data.Contains("Received sequence for"))
                .Run()
                .ConfigureAwait(false);

            var data = JObject.Parse(context.Data);
            var gauges = (JArray)data["Gauges"];
            var gauge = gauges.Single(c => c.Value<string>("Name").StartsWith("Received sequence for"));
            var tags = gauge["Tags"].ToObject<string[]>();

            Assert.AreEqual("queue-length.received", tags.GetTagValue("type"));
            Assert.AreEqual(SequenceKey, tags.GetTagValue("key"));
            Assert.AreEqual(SequenceValue, gauge.Value<string>("Value"));
        }


        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>((c,r) =>
                {
                    c.LimitMessageProcessingConcurrencyTo(1);
#pragma warning disable 618
                    c.EnableMetrics().SendMetricDataToServiceControl(MonitoringSpyAddress, TimeSpan.FromSeconds(5));
#pragma warning restore 618

                    c.Pipeline.Remove("DispatchQueueLengthBehavior");
                });
            }

            public class TestMessageHandler : IHandleMessages<TestMessage>
            {
                public QueueLengthContext TestContext { get; set; }

                public Task Handle(TestMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        public class TestMessage : ICommand
        {
        }
    }
}
