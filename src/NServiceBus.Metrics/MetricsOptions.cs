namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provides configuration options for Metrics feature
    /// </summary>
    public class MetricsOptions
    {
        /// <summary>
        /// Enables registering observers to available probes.
        /// </summary>
        /// <param name="register">Action that registers observers to probes</param>
        public void RegisterObservers(Action<ProbeContext> register)
        {
            Guard.AgainstNull(nameof(register), register);

            registerObservers += register;
        }

        internal void SetUpObservers(ProbeContext probeContext)
        {
            registerObservers(probeContext);
        }

        Action<ProbeContext> registerObservers = c => { };
    }
}