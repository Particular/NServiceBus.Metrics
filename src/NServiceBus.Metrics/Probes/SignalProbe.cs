using System;
using NServiceBus;

class SignalProbe(string name, string description) : Probe(name, description), ISignalProbe
{
    public void Register(OnEvent<SignalEvent> observer)
    {
        lock (Lock)
        {
            observers += observer;
        }
    }

    public void Register(Action observer) => Register((ref SignalEvent e) => observer());

    internal void Signal(ref SignalEvent e) => observers(ref e);

    volatile OnEvent<SignalEvent> observers = Empty;
    readonly object Lock = new();

    static void Empty(ref SignalEvent e) { }
}