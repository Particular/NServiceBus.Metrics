using System;
using NServiceBus;

class SignalProbe : Probe, ISignalProbe
{
    public SignalProbe(string id) : base(id)
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