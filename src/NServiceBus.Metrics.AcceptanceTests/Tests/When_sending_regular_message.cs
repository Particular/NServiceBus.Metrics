namespace NServiceBus.Metrics.AcceptanceTests.Tests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_regular_message : NServiceBusAcceptanceTest
    {
        const string InstanceId = "Metrics_instance_id_value";

        [Test]
        public async Task Should_include_metrics_custom_instance_id_as_a_header()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Sender>(b=>b.When(c=>c.Send(Conventions.EndpointNamingConvention(typeof(Receiver)), new Message())))
                .WithEndpoint<Receiver>()
                .Done(c=>c.NServiceBus_Metric_InstanceId_Header_Value == InstanceId)
                .Run()
                .ConfigureAwait(false);

            Assert.AreEqual(InstanceId, context.NServiceBus_Metric_InstanceId_Header_Value);
        }

        class Context : ScenarioContext
        {
            public string NServiceBus_Metric_InstanceId_Header_Value;
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    var metrics = c.EnableMetrics();
#pragma warning disable 618
                    metrics.SendMetricDataToServiceControl("non-existing-queue", TimeSpan.FromSeconds(1), InstanceId);
#pragma warning restore 618
                });
            }
        }

        public class Message : IMessage
        {
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                }).IncludeType<Message>();
            }

            public class MessageHandler : IHandleMessages<Message>
            {
                public Context TestContext { get; set; }

                public Task Handle(Message message, IMessageHandlerContext context)
                {
                    context.MessageHeaders.TryGetValue("NServiceBus.Metric.InstanceId", out var header);

                    TestContext.NServiceBus_Metric_InstanceId_Header_Value = header;

                    return Task.FromResult(0);
                }
            }
        }
    }
}