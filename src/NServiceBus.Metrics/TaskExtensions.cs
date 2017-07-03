namespace NServiceBus.Metrics
{
    using System.Threading.Tasks;

    class TaskExtensions
    {
        public static Task Completed => Task.FromResult(0);
    }
}