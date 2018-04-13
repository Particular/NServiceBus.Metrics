using System;
using NServiceBus;

class SignalProbe : Probe, ISignalProbe, IEventSensor<SignalEvent>
{
    public SignalProbe(string name, string description) : base(name, description)
    {
    }

    public void Register(OnEvent<SignalEvent> observer)
    {
        lock (Lock)
        {
            observers += observer;
        }
    }

    public void Register(Action observer)
    {
        Register((ref SignalEvent e) => observer());
    }

    public void Record(ref SignalEvent e)
    {
        observers(ref e);
    }

    volatile OnEvent<SignalEvent> observers = Empty;
    readonly object Lock = new object();

    static void Empty(ref SignalEvent e) { }
}