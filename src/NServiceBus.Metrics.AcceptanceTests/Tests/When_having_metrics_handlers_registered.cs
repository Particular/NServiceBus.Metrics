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

public class When_having_metrics_handlers_registered : NServiceBusAcceptanceTest
{
    HashSet<string> positiveProbes = new HashSet<string>
    {
        "Critical Time",
        "Processing Time",

        "# of msgs pulled from the input queue /sec",
        "# of msgs successfully processed / sec",
    };

    HashSet<string> errorProbes;

    public When_having_metrics_handlers_registered()
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
    public async Task Should_call_success_probes_on_success()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ConsumingReporter>(b => b.When(s => s.SendLocal(new MyMessage())))
            .Done(c => c.Values.Count >= positiveProbes.Count)
            .Run()
            .ConfigureAwait(false);

        foreach (var kvp in context.Values)
        {
            Console.WriteLine($"{kvp.Key}");

            Assert.True(positiveProbes.Contains(kvp.Key), $"Missing key {kvp.Key}");
            Assert.AreEqual(typeof(MyMessage).AssemblyQualifiedName, kvp.Value);
        }
    }

    [Test]
    public async Task Should_call_fail_probes_on_success()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<ThrowingReporter>(b => b.When(s => s.SendLocal(new MyMessage())).DoNotFailOnErrorMessages())
            .Done(c => c.Values.Count >= errorProbes.Count)
            .Run()
            .ConfigureAwait(false);

        foreach (var kvp in context.Values)
        {
            Console.WriteLine($"{kvp.Key}");

            Assert.True(errorProbes.Contains(kvp.Key), $"Missing key {kvp.Key}");
            Assert.AreEqual(typeof(MyMessage).AssemblyQualifiedName, kvp.Value);
        }
    }

    class Context : ScenarioContext
    {
        public Dictionary<string, string> Values = new Dictionary<string, string>();
    }

    class ConsumingReporter : EndpointConfigurationBuilder
    {
        public ConsumingReporter()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                var context = (Context)r.ScenarioContext;

                c.EnableMetrics().RegisterObservers(
                    ctx => { RegisterWritesToTestContext(ctx, context); });
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

    class ThrowingReporter : EndpointConfigurationBuilder
    {
        public ThrowingReporter()
        {
            EndpointSetup<DefaultServer>((c, r) =>
            {
                c.Recoverability().Immediate(immediate => immediate.NumberOfRetries(1));
                var context = (Context)r.ScenarioContext;

                c.EnableMetrics().RegisterObservers(
                    ctx => { RegisterWritesToTestContext(ctx, context); });
            });
        }


        public class MyHandler : IHandleMessages<MyMessage>
        {
            public Task Handle(MyMessage message, IMessageHandlerContext context)
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetException(new Exception());
                return tcs.Task;
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