using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NServiceBus.Metrics;
using NUnit.Framework;

public class When_receiving_message : NServiceBusAcceptanceTest
{

    const string KeyHeader = "NServiceBus.Metrics.QueueLength.Key";
    const string ValueHeader = "NServiceBus.Metrics.QueueLength.Value";
    const string SequenceValue = "42";
    static readonly string SequenceKey = Guid.NewGuid().ToString();

    protected class QueueLengthContext : ScenarioContext
    {
        public string Data { get; set; }

        public Func<bool> TrackReports = () => true;
    }
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

        public class MetricHandler : IHandleMessages<MetricReport>
        {
            public QueueLengthContext TestContext { get; set; }

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
    class Receiver : EndpointConfigurationBuilder
    {
        public Receiver()
        {
            EndpointSetup<DefaultServer>((c,r) =>
            {
                c.LimitMessageProcessingConcurrencyTo(1);
#pragma warning disable 618
                var address = NServiceBus.AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(MonitoringSpy));
                var metrics = c.EnableMetrics();
                metrics.SendMetricDataToServiceControl(address, TimeSpan.FromSeconds(5));
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