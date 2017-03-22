namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using global::Metrics.Json;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_reporting_to_ServiceControl : NServiceBusAcceptanceTest
    {
        static string MonitoringSpyAddress => Conventions.EndpointNamingConvention(typeof(MonitoringSpy));

        [Test]
        public async Task Should_send_reports_to_configured_queue()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>()
                .WithEndpoint<MonitoringSpy>()
                .Done(c => c.Report != null)
                .Run();

            Assert.IsNotNull(context.Report);
            Assert.AreEqual(Conventions.EndpointNamingConvention(typeof(Sender)), context.Report.Context);
        }

        class Context : ScenarioContext
        {
            public JsonMetricsContext Report { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableMetrics().SendMetricDataToServiceControl(MonitoringSpyAddress, TimeSpan.FromSeconds(1));
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
                    c.Conventions().DefiningMessagesAs(t => t.FullName == "Metrics.Json.JsonMetricsContext");
                });
            }

            public class MetricHandler : IHandleMessages<JsonMetricsContext>
            {
                public Context TestContext { get; set; }

                public Task Handle(JsonMetricsContext message, IMessageHandlerContext context)
                {
                    TestContext.Report = message;

                    return Task.FromResult(0);
                }
            }
        }
    }
}