namespace NServiceBus.Metrics.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;

    public class QueueLengthAcceptanceTests : NServiceBusAcceptanceTest
    {
        protected  static string MonitoringSpyAddress => AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(MonitoringSpy));

        protected class QueueLengthContext : ScenarioContext
        {
            public string Data { get; set; }
            public bool Handled { get; set; }
        }

        protected class MonitoringSpy : EndpointConfigurationBuilder
        {
            public MonitoringSpy()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    SerializationConfigExtensions.UseSerialization<NewtonsoftSerializer>(c);
                    MessageProcessingOptimizationExtensions.LimitMessageProcessingConcurrencyTo(c, 1);
                }).IncludeType<MetricReport>();
            }

            public class MetricHandler : IHandleMessages<MetricReport>
            {
                public QueueLengthContext TestContext { get; set; }

                public Task Handle(MetricReport message, IMessageHandlerContext context)
                {
                    TestContext.Data = message.Data.ToString();
                    TestContext.Handled = true;

                    return Task.FromResult(0);
                }
            }
        }
    }
}