using System;
using NServiceBus;

class DurationProbe : Probe, IDurationProbe
{
    public DurationProbe(string name, string description) : base(name, description)
    {
    }

    public void Register(OnEvent<DurationEvent> observer)
    {
        observers += observer;
    }

    public void Register(Action<TimeSpan> observer)
    {
        Register((ref DurationEvent e) => observer(e.Duration));
    }

    internal void Record(ref DurationEvent e)
    {
        observers(ref e);
    }

    OnEvent<DurationEvent> observers = Empty;

    static void Empty(ref DurationEvent e) { }
}