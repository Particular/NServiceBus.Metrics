namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using global::Newtonsoft.Json;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_reporting_to_ServiceControl : NServiceBusAcceptanceTest
    {
        static string MonitoringSpyAddress => Conventions.EndpointNamingConvention(typeof(MonitoringSpy));
        static Guid HostId = Guid.NewGuid();

        [Test]
        public async Task Should_send_reports_to_configured_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>()
                .WithEndpoint<MonitoringSpy>()
                .Done(c => c.Report != null)
                .Run()
                .ConfigureAwait(false);

            Assert.IsNotNull(context.Report);

            var metricsContext = context.Report["Context"].Value<string>();
            Assert.AreEqual($"{Conventions.EndpointNamingConvention(typeof(Sender))}", metricsContext);
            PayloadAssert.ContainsMeters(context.Report.ToString(Formatting.None));
            Assert.AreEqual(metricsContext, context.Headers[Headers.OriginatingEndpoint]);
            Assert.AreEqual(HostId.ToString("N"), context.Headers[Headers.OriginatingHostId]);
            Assert.AreEqual("NServiceBus.Metrics.MetricReport", context.Headers[Headers.EnclosedMessageTypes]);
            Assert.AreEqual(ContentTypes.Json, context.Headers[Headers.ContentType]);
        }

        class Context : ScenarioContext
        {
            public JObject Report { get; set; }

            public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(HostId);
#pragma warning disable 618
                    c.EnableMetrics().SendMetricDataToServiceControl(MonitoringSpyAddress, TimeSpan.FromSeconds(1));
#pragma warning restore 618
                });
            }
        }

        class MonitoringSpy : EndpointConfigurationBuilder
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
                public Context TestContext { get; set; }

                public Task Handle(MetricReport message, IMessageHandlerContext context)
                {
                    TestContext.Report = message.Data;
                    TestContext.Headers = context.MessageHeaders;

                    return Task.FromResult(0);
                }
            }
        }
    }
}

namespace NServiceBus.Metrics
{
    using global::Newtonsoft.Json.Linq;

    public class MetricReport : IMessage
    {
        public JObject Data { get; set; }
    }
}