using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;

class Program
{
    static void Main()
    {
        MainAsync("Sample.Endpoint").GetAwaiter().GetResult();
    }

    static async Task MainAsync(string endpointName)
    {
        Console.Title = endpointName;

        var endpointConfig = new EndpointConfiguration(endpointName);
        endpointConfig.SendFailedMessagesTo("error");
        endpointConfig.UsePersistence<InMemoryPersistence>();
        endpointConfig.UseSerialization<NewtonsoftSerializer>();
        
        var metrics = endpointConfig.EnableMetrics();
        metrics.EnableLogTracing(LogLevel.Info); // LogLevel.Debug is the default. Overriding to INFO just for the sample.
#pragma warning disable 618
        metrics.SendMetricDataToServiceControl("Sample.Endpoint");
#pragma warning restore 618

        var endpoint = await Endpoint.Start(endpointConfig)
            .ConfigureAwait(false);

        Console.WriteLine($"{endpointName} started.");
        Console.WriteLine("Press [ESC] to close. Any other key to send a message");

        while (Console.ReadKey(true).Key != ConsoleKey.Escape)
        {
            await endpoint.SendLocal(new SomeCommand())
                .ConfigureAwait(false);
        }

        Console.WriteLine($"{endpointName} shutting down");

        await endpoint.Stop()
            .ConfigureAwait(false);

        Console.WriteLine($"{endpointName} stopped");
    }
}