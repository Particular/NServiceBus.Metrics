using System;
using NServiceBus;

class SignalProbe : Probe, ISignalProbe
{
    public SignalProbe(string name, string description) : base(name, description)
    {
    }

    public void Register(Action observer)
    {
        observers += observer;
    }

    internal void Signal()
    {
        observers();
    }

    Action observers = () => { };
}