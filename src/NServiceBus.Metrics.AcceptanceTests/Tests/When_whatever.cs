namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_whatever : NServiceBusAcceptanceTest
    {
        static string ReceiverAddress => Conventions.EndpointNamingConvention(typeof(Receiver));
        static Guid HostId = Guid.NewGuid();

        [Test]
        public async Task Should_send_reports_to_configured_queue()
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
        }

        class Context : ScenarioContext
        {
            public ConcurrentBag<IReadOnlyDictionary<string, string>> Headers { get; set; } = new ConcurrentBag<IReadOnlyDictionary<string, string>>();
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.UniquelyIdentifyRunningInstance().UsingCustomIdentifier(HostId);
#pragma warning disable 618
                    c.EnableMetrics();
#pragma warning restore 618                    
                });
            }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestMessageHandler : IHandleMessages<TestMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(TestMessage message, IMessageHandlerContext context)
                {
                    TestContext.Headers.Add(context.MessageHeaders);

                    return Task.FromResult(0);
                }
            }
        }

        public class TestMessage : ICommand
        {
        }
    }
}