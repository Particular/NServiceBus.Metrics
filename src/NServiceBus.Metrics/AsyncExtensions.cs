namespace NServiceBus.Metrics
{
    using System.Threading.Tasks;

    static class AsyncExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Meant to be swallowed")]
        public static void IgnoreContinuation(this Task task)
        {
            //Noop
        }
    }
}