using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTests;
using NServiceBus.AcceptanceTests.EndpointTemplates;
using NUnit.Framework;

public class When_having_metrics_handlers_registered_after_startup : NServiceBusAcceptanceTest
{
    HashSet<string> positiveProbes = new HashSet<string>
    {
        "Critical Time",
        "Processing Time",

        "# of msgs pulled from the input queue /sec",
        "# of msgs successfully processed / sec",
    };

    HashSet<string> errorProbes;

    public When_having_metrics_handlers_registered_after_startup()
    {
        var probesNames = typeof(IProbe).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ProbePropertiesAttribute>() != null)
            .Select(t => t.GetCustomAttribute<ProbePropertiesAttribute>().Name)
            .ToArray();

        errorProbes = new HashSet<string>
        {
            "# of msgs pulled from the input queue /sec" // added explicitly as the message is pulled from the queue even in an error scenario
        };

        foreach (var name in probesNames)
        {
            if (positiveProbes.Contains(name) == false)
            {
                errorProbes.Add(name);
            }
        }
    }

    [Test]
    public async Task Should_call_SetUpObservers_when_new_observers_are_registered_after_startup_has_completed()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ConsumingReporter>(b => b.When(s => s.SendLocal(new MyMessage())))
            .Done(c => {
                var basicProbesRegistered = c.Values.Count >= positiveProbes.Count;
                if (basicProbesRegistered)
                {
                    c.MetricsOptions.RegisterObservers(probe =>
                    {
                        c.SecondSetupCalled = true;
                    });
                }

                return c.SecondSetupCalled;
            }) // basic startup done, need to register another observer
            .Run()
            .ConfigureAwait(false);

        foreach (var kvp in context.Values)
        {
            Console.WriteLine($"{kvp.Key}");

            Assert.True(positiveProbes.Contains(kvp.Key), $"Missing key {kvp.Key}");
            Assert.AreEqual(typeof(MyMessage).AssemblyQualifiedName, kvp.Value);
        }
    }

    class Context : ScenarioContext
    {
        public Dictionary<string, string> Values = new Dictionary<string, string>();
        public MetricsOptions MetricsOptions { get; set; }
        public bool SecondSetupCalled { get; set; }
    }

    class ConsumingReporter : EndpointConfigurationBuilder
    {
        public ConsumingReporter()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                var context = (Context)r.ScenarioContext;

                context.MetricsOptions = c.EnableMetrics();
                context.MetricsOptions.RegisterObservers(ctx => 
                {
                    RegisterWritesToTestContext(ctx, context);
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

    static void RegisterWritesToTestContext(ProbeContext ctx, Context context)
    {
        foreach (var duration in ctx.Durations)
        {
            duration.Register((ref DurationEvent e) => context.Values.Add(duration.Name, e.MessageType));
        }

        foreach (var signal in ctx.Signals)
        {
            signal.Register((ref SignalEvent e) => context.Values.Add(signal.Name, e.MessageType));
        }
    }

    public class MyMessage : IMessage
    {
    }
}