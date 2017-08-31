using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

public class Program
{
    public static async Task Main()
    {
        Console.Title = "Sample.Endpoint";

        var endpointConfig = new EndpointConfiguration("Sample.Endpoint");
        endpointConfig.SendFailedMessagesTo("error");
        endpointConfig.UseTransport<LearningTransport>();
        endpointConfig.UsePersistence<InMemoryPersistence>();
        endpointConfig.UseSerialization<NewtonsoftSerializer>();

        endpointConfig.UniquelyIdentifyRunningInstance().UsingCustomDisplayName("Sample.Instance");

        var metrics = endpointConfig.EnableMetrics();
#pragma warning disable 618
        // LogLevel.Debug is the default. Overriding to INFO just for the sample.
        metrics.EnableLogTracing(TimeSpan.FromSeconds(10), LogLevel.Info);
        metrics.SendMetricDataToServiceControl("Particular.ServiceControl.Monitoring", TimeSpan.FromSeconds(1));
#pragma warning restore 618

        var endpoint = await Endpoint.Start(endpointConfig)
            .ConfigureAwait(false);

        Console.WriteLine("Press [ESC] to close. Any other key to send a message");

        while (Console.ReadKey(true).Key != ConsoleKey.Escape)
        {
            await endpoint.SendLocal(new SomeCommand())
                .ConfigureAwait(false);
        }

        Console.WriteLine("shutting down");

        await endpoint.Stop()
            .ConfigureAwait(false);

        Console.WriteLine("stopped");
    }
}