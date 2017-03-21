namespace SampleEndpoint
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;

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

            var endpoint = await Endpoint.Start(endpointConfig);

            Console.WriteLine($"{endpointName} started. Press [ESC] to close");

            while (Console.ReadKey().Key != ConsoleKey.Escape)
            {
                await endpoint.SendLocal(new SomeCommand());
                await Task.Delay(100);
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
