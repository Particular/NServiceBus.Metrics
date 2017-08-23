using NServiceBus;

class SignalProbe : Probe, ISignalProbe
{
    public SignalProbe(string name, string description) : base(name, description)
    {
    }

    public void Register(OnEvent<SignalEvent> observer)
    {
        observers += observer;
    }

    internal void Signal(ref SignalEvent e)
    {
        observers(ref e);
    }

    OnEvent<SignalEvent> observers = Empty;

    static void Empty(ref SignalEvent e) { }
}