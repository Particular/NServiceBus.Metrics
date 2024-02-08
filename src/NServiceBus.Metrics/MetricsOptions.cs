namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Faults;

    /// <summary>
    /// Provides configuration options for Metrics feature
    /// </summary>
    public class MetricsOptions
    {
        internal Func<ImmediateRetryMessage, CancellationToken, Task> Immediate { get; set; } = (_, _) => Task.CompletedTask;
        internal Func<DelayedRetryMessage, CancellationToken, Task> Delayed { get; set; } = (_, _) => Task.CompletedTask;

        /// <summary>
        /// Enables registering observers to available probes.
        /// </summary>
        /// <param name="register">Action that registers observers to probes</param>
        public void RegisterObservers(Action<ProbeContext> register)
        {
            ArgumentNullException.ThrowIfNull(register);

            registerObservers += register;
        }

        internal void SetUpObservers(ProbeContext probeContext) => registerObservers(probeContext);

        Action<ProbeContext> registerObservers = _ => { };
    }
}