namespace SampleEndpoint
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Metrics;

    class Program
    {
        static void Main(string[] args)
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
            metrics.EnableMetricTracing();

            var endpoint = await Endpoint.Start(endpointConfig);

            Console.WriteLine($"{endpointName} started.");
            Console.WriteLine("Press [ESC] to close. Any other key to send a message");

            while (Console.ReadKey(true).Key != ConsoleKey.Escape)
            {
                await endpoint.SendLocal(new SomeCommand());
            }

            Console.WriteLine($"{endpointName} shutting down");

            await endpoint.Stop();

            Console.WriteLine($"{endpointName} stopped");
        }
    }

    class SomeCommand : ICommand { }

    class SomeCommandHandler : IHandleMessages<SomeCommand>
    {
        public Task Handle(SomeCommand message, IMessageHandlerContext context) => Task.CompletedTask;
    }
}
