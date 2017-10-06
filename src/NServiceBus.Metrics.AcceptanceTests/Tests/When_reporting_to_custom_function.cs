using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
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
        PayloadAssert.ContainsMeters(context.Data, meters);
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
                var context = (Context)r.ScenarioContext;

#pragma warning disable 618
                c.EnableMetrics()
                    .EnableCustomReport(payload =>
                    {
                        context.Data = payload;
                        return Task.FromResult(0);
                    }, TimeSpan.FromMilliseconds(5));
            });
#pragma warning restore 618
        }
    }

    static List<string> meters = new List<string>
    {
        "# of msgs failures / sec",
        "# of msgs pulled from the input queue /sec",
        "# of msgs successfully processed / sec",
        "Retries",
        "Critical Time",
        "Processing Time"
    };
}
