using System;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_using_metrics_with_sendonly_endpoint : NServiceBusAcceptanceTest
{
    [Test]
    public void Should_fail()
    {
        var exception = Assert.CatchAsync<Exception>(async () =>
        {
            await Scenario.Define<Context>()
                .WithEndpoint<SendOnlyEndpoint>()
                .Done(c => c.EndpointsStarted)
                .Run();
        });

        Assert.That(exception!.Message, Does.Contain("Metrics are not supported on send only endpoints"));
    }

    class Context : ScenarioContext
    {
    }

    class SendOnlyEndpoint : EndpointConfigurationBuilder
    {
        public SendOnlyEndpoint() =>
            EndpointSetup<DefaultServer>((c, _) =>
            {
                c.SendOnly();

                c.EnableMetrics();
            });
    }
}