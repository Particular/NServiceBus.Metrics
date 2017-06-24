namespace NServiceBus
{
    using System;

    class SignalProbe : Probe, ISignalProbe
    {
        public SignalProbe(string name, string description) : base(name, description)
        {
        }

        internal void Signal()
        {
            observers();
        }

        public void Register(Action observer)
        {
            observers += observer;
        }

        Action observers = () => { };
    }
}