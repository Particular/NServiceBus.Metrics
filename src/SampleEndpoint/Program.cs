using System;
using System.Threading.Tasks;
using NServiceBus;

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

        var metrics = endpointConfig.EnableMetrics();
        metrics.EnableLogTracing(TimeSpan.FromSeconds(10));

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

        await endpoint.Stop().ConfigureAwait(false);

        Console.WriteLine($"{endpointName} stopped");
    }
}