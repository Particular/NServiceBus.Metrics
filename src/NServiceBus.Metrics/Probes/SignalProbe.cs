using System;
using NServiceBus;

class SignalProbe : Probe, ISignalProbe
{
    public SignalProbe(string id, string name) : base(id, name)
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