namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_reporting_to_custom_function : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_call_defined_func()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Reporter>()
                .Done(c => c.Data != null)
                .Run()
                .ConfigureAwait(false);

            Assert.IsNotNull(context.Data);
            PayloadAssert.ContainsMeters(context.Data);
        }

        class Context : ScenarioContext
        {
            public string Data { get; set; }
        }

        class Reporter : EndpointConfigurationBuilder
        {
            public Reporter()
            {
                EndpointSetup<DefaultServer>((c, r) =>
                {
                    var context = (Context) r.ScenarioContext;

                    c.EnableMetrics(TimeSpan.FromMilliseconds(5))
                        .EnableCustomReport(payload =>
                        {
                            context.Data = payload;
                            return Task.FromResult(0);
                        });
                });
            }
        }
    }
}