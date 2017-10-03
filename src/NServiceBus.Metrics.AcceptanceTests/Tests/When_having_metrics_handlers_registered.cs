using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_having_metrics_handlers_registered : NServiceBusAcceptanceTest
{
    static readonly HashSet<string> probes = new HashSet<string>
    {
        "Critical Time",
        "Processing Time",
        //"Retries",
        "# of msgs pulled from the input queue /sec",
        "# of msgs successfully processed / sec",
        //"# of msgs failures / sec"
    };

    [Test]
    public async Task Should_call_probe_handlers()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Reporter>(b=>b.When(s=>s.SendLocal(new MyMessage())))
            .Done(c => c.Values.Count >= probes.Count)
            .Run()
            .ConfigureAwait(false);

        foreach (var kvp in context.Values)
        {
            Console.WriteLine($"{kvp.Key}");

            Assert.True(probes.Contains(kvp.Key));
            Assert.AreEqual(typeof(MyMessage).AssemblyQualifiedName, kvp.Value);
        }
    }

    class Context : ScenarioContext
    {
        public Dictionary<string, string> Values = new Dictionary<string, string>();
    }

    class Reporter : EndpointConfigurationBuilder
    {
        public Reporter()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                var context = (Context) r.ScenarioContext;

                c.EnableMetrics().RegisterObservers(
                    ctx =>
                    {
                        foreach (var duration in ctx.Durations)
                        {
                            duration.Register((ref DurationEvent e) => context.Values.Add(duration.Name, e.MessageType));
                        }

                        foreach (var signal in ctx.Signals)
                        {
                            signal.Register((ref SignalEvent e) => context.Values.Add(signal.Name, e.MessageType));
                        }
                    });
            });
        }

        public class MyHandler : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                return Task.Delay(100);
            }
        }
    }

    public class MyMessage : IMessage
    {
    }
}